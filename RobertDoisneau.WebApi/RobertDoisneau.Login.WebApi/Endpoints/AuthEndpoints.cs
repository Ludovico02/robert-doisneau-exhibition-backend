using RobertDoisneau.Login.WebApi.Models;
using RobertDoisneau.Login.WebApi.Services;
using System.Security.Claims;

namespace RobertDoisneau.Login.WebApi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder route)
    {
        var group = route.MapGroup("/api/auth");

        // Sign in
        group.MapPost("/login", async (LoginRequestHtml request, UserService userService) =>
        {
            var user = await userService.GetByUsernameAsync(request.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new { message = "Login successfull!", userId = user.Id, token = "" });
        });

        // Sign up
        group.MapPost("/register", async (RegisterRequestHtml request, UserService userService) =>
        {

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { error = "Username e password sono obbligatori." });
            }

            var utenteEsistente = await userService.GetByUsernameAsync(request.Username);
            if (utenteEsistente != null)
            {
                return Results.BadRequest(new { error = "Questo username è già in uso. Scegline un altro." });
            }

            string passwordCriptata = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var nuovoUtente = new User
            {
                Username = request.Username,
                PasswordHash = passwordCriptata,
                Email = request.Email,
                CreationDate = DateTime.UtcNow
            };

            try
            {
                await userService.AddUserAsync(nuovoUtente);
                return Results.Ok(new { message = "Registrazione completata con successo!", userId = nuovoUtente.Id });
            }
            catch (Exception)
            {
                return Results.Problem("Errore interno durante il salvataggio nel database.", statusCode: 500);
            }
        });
    }

}
