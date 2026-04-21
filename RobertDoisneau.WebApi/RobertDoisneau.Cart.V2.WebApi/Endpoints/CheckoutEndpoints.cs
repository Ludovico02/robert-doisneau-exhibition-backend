using RobertDoisneau.Cart.V2.WebApi.Models;
using RobertDoisneau.Cart.V2.WebApi.Services;
using System.Security.Claims;

namespace RobertDoisneau.Cart.V2.WebApi.Endpoints;

public static class CheckoutEndpoints
{
    public static void MapCheckoutEndpoints(this IEndpointRouteBuilder route)
    {
        var group = route.MapGroup("/api/checkout").WithTags("Checkout");

        group.MapPost("/buy", ProcessCheckoutAsync).RequireAuthorization();
    }

    public static async Task<IResult> ProcessCheckoutAsync(
        CheckoutRequest request, CheckoutService checkoutService, HttpContext httpContext)
    {
        var userIdString = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            // Console.WriteLine(string.IsNullOrEmpty(userIdString));
            return Results.Unauthorized();
        }

        if (request == null || request.Items == null || !request.Items.Any())
        {
            return Results.BadRequest(new { message = "Il carrello è vuoto o la richiesta non è valida." });
        }

        var success = await checkoutService.ProcessPurchaseAsync(userId, request.Items);

        if (!success)
        {
            return Results.Conflict(new { message = "Ci dispiace, i biglietti richiesti sono esauriti o non più disponibili." });
        }

        return Results.Ok(new { message = "Acquisto completato con successo! I tuoi biglietti sono stati generati." });
    }
}

