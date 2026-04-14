using RobertDoisneau.Cart.V2.WebApi.Services;

namespace RobertDoisneau.Cart.V2.WebApi.Endpoints;

public static class ExhibitionsEndpoints
{
    public static void MapExhibitionsEndpoints(this IEndpointRouteBuilder route)
    {
        var group = route.MapGroup("/api/exhibitions").WithTags("Exhibitions");

        // Risponderà a GET /api/exhibitions
        group.MapGet("/", GetActiveExhibitionsAsync);
    }

    public static async Task<IResult> GetActiveExhibitionsAsync(ExhibitionService exhibitionService)
    {
        var exhibitions = await exhibitionService.GetAllActiveExhibitionsAsync();

        return exhibitions.Any()
            ? Results.Ok(exhibitions)
            : Results.NotFound(new { message = "Al momento non ci sono mostre disponibili." });
    }
}
