using Microsoft.AspNetCore.Http.HttpResults;
using RobertDoisneau.Cart.WebApi.Services;

namespace RobertDoisneau.Cart.WebApi.Endpoints;

public static class CartEndpoints
{
    public static void MapCartEndpoints(this IEndpointRouteBuilder route)
    {
        var group = route.MapGroup("/api/cart")
                         .WithTags("Cart");

        group.MapPost("/add", AddToCartAsync)
            .WithName("AddToCart")
            .WithSummary("Aggiunge un biglietto al carrello o aggiorna la quantità se già presente.");

        group.MapGet("/total/{userId:int}", GetTotalAsync)
            .WithName("GetCartTotal");

        group.MapPost("/cleanup", ManualCleanupAsync)
            .WithName("ManualCleanup")
            .WithSummary("Rimuove manualmente i carrelli scaduti.");
    }

    /// <summary>
    /// Aggiunge un elemento al carrello (Logica UPSERT)
    /// </summary>
    public static async Task<Results<Ok<object>, BadRequest<string>>> AddToCartAsync(ICartService cartService, CartRequest request)
    {
        // Validazione minima
        if (request.Quantita <= 0)
            return TypedResults.BadRequest("La quantità deve essere maggiore di zero.");

        var success = await cartService.AddToCartAsync(
            request.UserId,
            request.MostraId,
            request.Quantita,
            request.DataVisita
        );

        if (!success)
            return TypedResults.BadRequest("Errore durante l'aggiunta al carrello.");

        return TypedResults.Ok((object)new { Message = "Operazione completata con successo" });
    }

    /// <summary>
    /// Ottiene il totale dei biglietti per un utente
    /// </summary>
    public static async Task<Ok<object>> GetTotalAsync(ICartService cartService, int userId)
    {
        var total = await cartService.GetTotalTicketsAsync(userId);
        return TypedResults.Ok((object)new { UserId = userId, TotalTickets = total });
    }

    /// <summary>
    /// Esegue la pulizia manuale (comodo per i test prima di aspettare Hangfire)
    /// </summary>
    public static async Task<Ok<object>> ManualCleanupAsync(ICartService cartService, int minutes = 30)
    {
        var deletedRows = await cartService.CleanupExpiredCartsAsync(minutes);
        return TypedResults.Ok((object)new { DeletedRows = deletedRows, Timestamp = DateTime.UtcNow });
    }
}

// DTO per la richiesta
public record CartRequest(int UserId, int MostraId, int Quantita, DateTime DataVisita);
