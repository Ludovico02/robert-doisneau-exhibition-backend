using RobertDoisneau.Cart.V2.WebApi.Services;

namespace RobertDoisneau.Cart.V2.WebApi.Endpoints;

public static class ExhibitionsEndpoints
{
    public static void MapExhibitionsEndpoints(this IEndpointRouteBuilder route)
    {
        var group = route.MapGroup("/api/exhibitions").WithTags("Exhibitions");

        // Risponderà a GET /api/exhibitions
        group.MapGet("/", GetActiveExhibitionsAsync);
        group.MapGet("/{id:int}", GetExhibitionCategoriesAsync);
    }

    public static async Task<IResult> GetActiveExhibitionsAsync(ExhibitionService exhibitionService)
    {
        var exhibitions = await exhibitionService.GetAllActiveExhibitionsAsync();

        return exhibitions.Any()
            ? Results.Ok(exhibitions)
            : Results.NotFound(new { message = "Al momento non ci sono mostre disponibili." });
    }

    public static async Task<IResult> GetExhibitionCategoriesAsync(int id, ExhibitionService exhibitionService)
    {
        var categories = await exhibitionService.GetTicketCategoriesByExhibitionIdAsync(id);

        return categories.Any()
            ? Results.Ok(categories)
            : Results.NotFound(new { message = "Nessuna categoria trovata per questa mostra." });
    }
}
