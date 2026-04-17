using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Security.Claims; // FONDAMENTALE: per leggere il token
using RobertDoisneau.Cart.V2.WebApi.Services;

namespace RobertDoisneau.Cart.V2.WebApi.Endpoints;

public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this IEndpointRouteBuilder route)
    {
        var group = route.MapGroup("/api/tickets").WithTags("Tickets");

        group.MapGet("/my-tickets", GetTicketsAsync);
    }

    public static async Task<IResult> GetTicketsAsync(HttpContext httpContext, TicketService ticketService)
    {
        // Estraiamo l'ID dell'utente loggato dal Token JWT
        var userIdString = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        //foreach (var claim in httpContext.User.Claims)
        //{
        //    Console.WriteLine($"Type: {claim.Type} - Value: {claim.Value}");
        //}

        // Se il token è manomesso o manca l'ID, lo blocchiamo
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            return Results.Unauthorized();
        }

        // Ora usiamo l'ID sicuro letto dal token per cercare i biglietti nel DB
        var tickets = await ticketService.GetUserTicketsAsync(userId);

        // Restituisce sempre 200 OK, anche se la lista è vuota
        return Results.Ok(tickets);
    }
}