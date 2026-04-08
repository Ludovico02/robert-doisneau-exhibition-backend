using RobertDoisneau.WebApi.GalleryAPI.EndPoints;
using RobertDoisneau.WebApi.GalleryAPI.Services;


var builder = WebApplication.CreateBuilder(args);



builder.Services.AddOpenApi();

builder.Services.AddScoped<IPhotoService, PhotoService>();

var app = builder.Build();


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

