using Microsoft.AspNetCore.Identity.Data;
using RobertDoisneau.Login.WebApi.Endpoints;
using RobertDoisneau.Login.WebApi.Models;
using RobertDoisneau.Login.WebApi.Services;

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

app.MapAuthEndpoints();

app.Run();

