using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using RobertDoisneau.WebApi.GalleryAPI.EndPoints;
using RobertDoisneau.WebApi.GalleryAPI.Services;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://127.0.0.1:5500", "http://localhost:5500" };

//SERVIZIO PER LIMITARE LE RICHIESTE (RATE LIMITER)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests; 

    options.AddFixedWindowLimiter("fixed-policy", opt =>
    {
        opt.Window = TimeSpan.FromSeconds(10); 
        opt.PermitLimit = 5; 
        opt.QueueLimit = 0; 
    });
});


builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader().AllowCredentials());
});

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "Configuration value 'Jwt:Key' is missing or shorter than 32 characters. Run scripts/setup-dev.ps1 (or .sh).");
}

// Token JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.IncludeErrorDetails = builder.Environment.IsDevelopment();
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

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["X-Access-Token"];
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddOpenApi();

builder.Services.AddAuthorization();

builder.Services.AddScoped<IPhotoService, PhotoService>();

var app = builder.Build();

app.UseCors("Frontend");
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "GALLERY API v1");
    });
}
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () =>
{
    return "HOME, vai su /api/gallery per vedere la lista delle foto! ";
});

app.MapPhotoEndpoints();

app.Run();
