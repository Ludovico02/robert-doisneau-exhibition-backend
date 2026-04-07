using Microsoft.AspNetCore.Identity.Data;
using RobertDoisneau.Login.WebApi.Services;
using RobertDoisneau.Login.WebApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermettiTutto", policy =>
    {
        policy.AllowAnyOrigin()   // Accetta richieste da qualsiasi pagina HTML
              .AllowAnyMethod()   // Accetta POST, GET, ecc.
              .AllowAnyHeader();  // Accetta qualsiasi tipo di dato (JSON)
    });
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<UserService>();

var app = builder.Build();

app.UseCors("PermettiTutto");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Sign in
app.MapPost("/api/auth/login", async (LoginRequestHtml request, UserService userService) =>
{
    var user = await userService.GetByUsernameAsync(request.Username);

    if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new { message = "Login successfull!", userId = user.Id });
});

// Sign up
app.MapPost("/api/auth/register", async (RegisterRequestHtml request, UserService userService) =>
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

app.Run();

