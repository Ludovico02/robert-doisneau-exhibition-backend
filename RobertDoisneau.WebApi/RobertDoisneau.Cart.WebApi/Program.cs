using Hangfire;
using Hangfire.PostgreSql;
using RobertDoisneau.Cart.WebApi.Endpoints;
using RobertDoisneau.Cart.WebApi.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
    {
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("db"));
    }));

builder.Services.AddHangfireServer();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<ICartService, CartService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    // NOTA: Usiamo un'espressione lambda per chiamare il servizio
    // Hangfire risolverà automaticamente ICartService dal contenitore delle dipendenze
    recurringJobManager.AddOrUpdate<ICartService>(
        "pulizia-carrello-scaduto",
        service => service.CleanupExpiredCartsAsync(1), // Parametro: minuti di scadenza
        "* * * * *"
    );
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Cart Test");
    });

    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapCartEndpoints();

app.Run();
