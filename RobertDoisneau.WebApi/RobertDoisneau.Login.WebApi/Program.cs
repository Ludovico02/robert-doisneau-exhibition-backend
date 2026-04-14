using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity.Data;
using RobertDoisneau.Login.WebApi.Endpoints;
using RobertDoisneau.Login.WebApi.Models;
using RobertDoisneau.Login.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermettiTutto", policy =>
    {
        // Cambiare l'allow any origin con il link del frontend
        policy.AllowAnyOrigin()   // Accetta richieste da qualsiasi pagina HTML
              .AllowAnyMethod()   // Accetta POST, GET, ecc.
              .AllowAnyHeader();  // Accetta qualsiasi tipo di dato (JSON)
    });
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<UserService>();

// Token JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(jwtOptions =>
    {
        jwtOptions.Authority = builder.Configuration["Jwt:Authority"];
    });

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

