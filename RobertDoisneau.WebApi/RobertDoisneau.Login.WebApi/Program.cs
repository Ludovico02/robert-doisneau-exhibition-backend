using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RobertDoisneau.Login.WebApi.Endpoints;
using RobertDoisneau.Login.WebApi.Services;
using System.Text;

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
builder.Services.AddScoped<JWTService>();

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

//JWT Authentication
var jwtKey = builder.Configuration.GetValue<string>("Jwt:Key");
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("Configuration value 'Jwt:Key' is missing or empty. Set it in appsettings or environment variables.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration.GetValue<string>("Jwt:Issuer"),
            ValidAudience = builder.Configuration.GetValue<string>("Jwt:Audience"),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

app.UseAuthentication();

app.UseAuthorization();

app.UseHttpsRedirection();

app.MapAuthEndpoints();

app.Run();

