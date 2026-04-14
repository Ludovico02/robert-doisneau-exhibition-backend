using RobertDoisneau.Cart.V2.WebApi.Endpoints;
using RobertDoisneau.Cart.V2.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Services.AddScoped<ExhibitionService>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<CheckoutService>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v2");
    });
}

app.UseHttpsRedirection();

app.MapCheckoutEndpoints();
app.MapUsersEndpoints(); 
app.MapExhibitionsEndpoints();

app.Run();
