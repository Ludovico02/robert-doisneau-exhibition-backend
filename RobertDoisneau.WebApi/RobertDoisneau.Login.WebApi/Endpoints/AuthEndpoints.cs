using Npgsql;
using RobertDoisneau.Login.WebApi.Models;
using RobertDoisneau.Login.WebApi.Services;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace RobertDoisneau.Login.WebApi.Endpoints;

public static class AuthEndpoints
{
    private const int MinPasswordLength = 8;
    private const int MaxPasswordBytes = 72;
    private static readonly Regex UsernameRegex = new("^[a-zA-Z0-9]{3,50}$", RegexOptions.Compiled);
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword("timing-equalizer-not-a-real-password");

    public static void MapAuthEndpoints(this IEndpointRouteBuilder route)
    {
        var group = route.MapGroup("/api/auth");

        group.MapPost("/login", LoginAsync)
            .RequireRateLimiting("LoginRateLimit");

        group.MapPost("/register", RegisterAsync)
            .RequireRateLimiting("RegisterRateLimit");

        group.MapPost("/logout", (HttpContext context) =>
        {
            context.Response.Cookies.Delete("X-Access-Token", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/"
            });
            return Results.Ok(new { message = "Logged out successfully" });
        });
    }

    public static async Task<IResult> LoginAsync(
        LoginRequestHtml request, UserService userService, JWTService jwtService, HttpContext httpContext)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return Results.BadRequest(new { error = "Username and password are required." });
        }

        if (Encoding.UTF8.GetByteCount(request.Password) > MaxPasswordBytes)
        {
            return Results.BadRequest(new { error = $"The password is too long (maximum {MaxPasswordBytes} bytes)." });
        }

        var user = await userService.GetByUsernameAsync(request.Username.Trim());
        var passwordOk = BCrypt.Net.BCrypt.Verify(request.Password, user?.PasswordHash ?? DummyHash);
        if (user is null || !passwordOk)
        {
            return Results.Unauthorized();
        }

        var token = jwtService.GenerateToken(user);

        // Configurazione del cookie
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTime.UtcNow.AddMinutes(30)
        };

        httpContext.Response.Cookies.Append("X-Access-Token", token, cookieOptions);

        return Results.Ok(new { message = "Login successful!" });
    }

    public static async Task<IResult> RegisterAsync(
        RegisterRequestHtml request, UserService userService, ILoggerFactory loggerFactory)
    {
        var username = (request.Username ?? "").Trim();
        var email = (request.Email ?? "").Trim().ToLowerInvariant();
        var password = request.Password ?? "";

        var validationError = ValidateRegistration(username, email, password);
        if (validationError is not null)
            return Results.BadRequest(new { error = validationError });

        if (await userService.GetByUsernameAsync(username) is not null)
            return Results.Conflict(new { error = "This username is already in use. Please choose another one." });

        if (await userService.GetByEmailAsync(email) is not null)
            return Results.Conflict(new { error = "An account with this email already exists." });

        var passwordHashed = BCrypt.Net.BCrypt.HashPassword(password);

        var newUser = new User
        {
            Username = username,
            PasswordHash = passwordHashed,
            Email = email,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await userService.AddUserAsync(newUser);
            return Results.Ok(new { message = "Registration completed successfully!", userId = newUser.Id });
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            var isEmail = ex.ConstraintName?.Contains("email", StringComparison.OrdinalIgnoreCase) == true;
            return Results.Conflict(new
            {
                error = isEmail
                    ? "An account with this email already exists."
                    : "This username is already in use. Please choose another one."
            });
        }
        catch (Exception ex)
        {
            loggerFactory.CreateLogger("RobertDoisneau.Login.Auth")
                .LogError(ex, "Unexpected error while registering user {Username}", username);
            return Results.Problem("An internal error occurred while saving the user.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static string? ValidateRegistration(string username, string email, string password)
    {
        if (!UsernameRegex.IsMatch(username))
            return "The username must be 3-50 characters long and contain only letters and numbers.";
        if (email.Length is 0 or > 100 || !MailAddress.TryCreate(email, out var parsed) || parsed.Address != email)
            return "Please enter a valid email address.";
        if (password.Length < MinPasswordLength)
            return $"The password must be at least {MinPasswordLength} characters long.";
        if (Encoding.UTF8.GetByteCount(password) > MaxPasswordBytes)
            return $"The password is too long (maximum {MaxPasswordBytes} bytes).";
        return null;
    }
}
