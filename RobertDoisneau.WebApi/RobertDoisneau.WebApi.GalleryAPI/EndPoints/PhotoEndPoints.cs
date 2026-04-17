namespace RobertDoisneau.WebApi.GalleryAPI.EndPoints;

using Microsoft.AspNetCore.Http.HttpResults;
using RobertDoisneau.WebApi.GalleryAPI.Models;
using RobertDoisneau.WebApi.GalleryAPI.Services;
public static class PhotoEndPoints
{
    public static void MapPhotoEndpoints(this IEndpointRouteBuilder route)
    {

        var group = route.MapGroup("/api/gallery")
            .WithTags("Gallery")
            .RequireAuthorization();


        group.MapGet("", GetGalleryAsync)
        .WithName("Get Gallery");

        group.MapGet("{id:int}", GetPhotoAsync)
        .WithName("GetById");

    }
    /// <summary>
    /// METODO PER VISUALIZZARE LA GALLERIA, RITORNA UNA LISTA DI FOTO
    /// </summary>
    /// <param name="photoService"></param>
    /// <returns></returns>
    public static async Task<Ok<IEnumerable<Photo>>> GetGalleryAsync(IPhotoService photoService)
    {


        var list = await photoService.GetListAsync();
        return TypedResults.Ok(list);
    }
    /// <summary>
    /// METODO PER VISUALIZZARE UNA FOTO IN BASE ALL'ID, RITORNA LA FOTO SE TROVATA, ALTRIMENTI RITORNA NOTFOUND
    /// </summary>
    /// <param name="id"></param>
    /// <param name="photoService"></param>
    /// <returns></returns>
    public static async Task<Results<Ok<Photo>, NotFound>> GetPhotoAsync(int id, IPhotoService photoService)
    {
        var photo = await photoService.GetByIdAsync(id);
        if (photo is null)
            return TypedResults.NotFound();
        return TypedResults.Ok(photo);
    }
}
