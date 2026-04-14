using RobertDoisneau.Cart.V2.WebApi.Services;

namespace RobertDoisneau.Cart.V2.WebApi.Endpoints;

public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this IEndpointRouteBuilder route)
    {
        // Qui potresti aggiungere endpoint per la gestione degli utenti, ad esempio:
        // group.MapPost("/register", RegisterUserAsync);
        // group.MapPost("/login", LoginUserAsync);
        route.MapGet("/api/tickets/user/{userId:int}", GetTicketsAsync);
    }

    public static async Task<IResult> GetTicketsAsync(int userId, TicketService ticketService)
    {
            if (userId <= 0)
                return Results.BadRequest(new { message = "ID utente non valido." });

            var tickets = await ticketService.GetUserTicketsAsync(userId);

            // Restituisce sempre 200 OK, anche se la lista è vuota (è il comportamento REST corretto)
            return Results.Ok(tickets);
    }
}
