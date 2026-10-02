using System.Security.Claims;
using RobertDoisneau.Cart.V2.WebApi.Services;

namespace RobertDoisneau.Cart.V2.WebApi.Endpoints;

public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this IEndpointRouteBuilder route)
    {
        var group = route.MapGroup("/api/tickets").WithTags("Tickets").RequireAuthorization();

        group.MapGet("/my-tickets", GetTicketsAsync);
    }

    public static async Task<IResult> GetTicketsAsync(HttpContext httpContext, TicketService ticketService)
    {
        var userIdString = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            return Results.Unauthorized();
        }

        var tickets = await ticketService.GetUserTicketsAsync(userId);

        return Results.Ok(tickets);
    }
}