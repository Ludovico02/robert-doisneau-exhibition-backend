using Microsoft.AspNetCore.Http.HttpResults;
using RobertDoisneau.Cart.V2.WebApi.Models;
using RobertDoisneau.Cart.V2.WebApi.Services;

namespace RobertDoisneau.Cart.V2.WebApi.Endpoints;

public static class ExhibitionsEndpoints
{

    public static void MapExhibitionsEndpoints(this IEndpointRouteBuilder route)
    {

        var group = route.MapGroup("/api/exhibitions")
            .WithTags("Exhibitions");


        group.MapGet("", GetExhibitionsList)
        .WithName("Get Exhibitions");

    }



    public static async Task<Ok<IEnumerable<Exhibition>>> GetExhibitionsList(ExhibitionService exhibitionService)
    {


        var list = await exhibitionService.GetListAsync();
        return TypedResults.Ok(list);
    }



}
