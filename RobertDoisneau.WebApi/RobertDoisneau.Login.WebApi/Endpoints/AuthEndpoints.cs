using RobertDoisneau.Login.WebApi.Models;
using RobertDoisneau.Login.WebApi.Services;
using System.Text.RegularExpressions;

namespace RobertDoisneau.Login.WebApi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder route)
    {
        var group = route.MapGroup("/api/auth");

        group.MapPost("/login", LoginAsync);

        group.MapPost("/register", RegisterAsync);

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
        var user = await userService.GetByUsernameAsync(request.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
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

    public static async Task<IResult> RegisterAsync(RegisterRequestHtml request, UserService userService)
    {

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Username and password are mandatory." });
        }

        if (!Regex.IsMatch(request.Username, @"^[a-zA-Z0-9]+$"))
        {
            return Results.BadRequest(new { error = "The username can only contain letters and numbers." });
        }
       
        var existingUser = await userService.GetByUsernameAsync(request.Username);
        if (existingUser != null)
        {
            return Results.BadRequest(new { error = "This username is already in use. Please choose another one." });
        }

        string passwordHashed = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var newUser = new User
        {
            Username = request.Username,
            PasswordHash = passwordHashed,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await userService.AddUserAsync(newUser);
            return Results.Ok(new { message = "Registration completed successfully!", userId = newUser.Id });
        }
        catch (Exception)
        {
            return Results.Problem("Internal error occurred while saving to the database.", statusCode: 500);
        }
    }
}
