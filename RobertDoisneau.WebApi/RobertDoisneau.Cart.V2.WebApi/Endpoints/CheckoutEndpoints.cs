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
            return Results.Unauthorized();
        }

        if (!CheckoutRules.TryNormalize(request?.Items, out var items, out var error))
            return Results.BadRequest(new { message = error });

        var result = await checkoutService.ProcessPurchaseAsync(userId, items);

        return result switch
        {
            PurchaseResult.Success => Results.Ok(new { message = "Purchase completed successfully. Your tickets have been generated." }),
            PurchaseResult.SoldOut => Results.Conflict(new { message = "Sorry, the requested tickets are sold out or no longer available." }),
            PurchaseResult.NotFound => Results.NotFound(new { message = "One of the requested exhibitions does not exist." }),
            PurchaseResult.InvalidRequest => Results.BadRequest(new { message = "The request is not valid." }),
            _ => Results.Problem("An unexpected error occurred while processing the purchase.", statusCode: StatusCodes.Status500InternalServerError)
        };
    }
}
