using Microsoft.AspNetCore.RateLimiting;
using RobertDoisneau.WebApi.GalleryAPI.EndPoints;
using RobertDoisneau.WebApi.GalleryAPI.Services;


var builder = WebApplication.CreateBuilder(args);

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
options.AddPolicy("PermettiTutto", policy =>
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader()
          ));

builder.Services.AddOpenApi();

builder.Services.AddScoped<IPhotoService, PhotoService>();

var app = builder.Build();

app.UseRateLimiter();
app.UseCors("PermettiTutto");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "GALLERY API v1");
    });
}
app.UseHttpsRedirection();

app.MapGet("/", () =>
{
    return "HOME, vai su /api/gallery per vedere la lista delle foto! ";
});

app.MapPhotoEndpoints();

app.Run();

