namespace RobertDoisneau.WebApi.GalleryAPI.Services;
using RobertDoisneau.WebApi.GalleryAPI.Models;

public interface IPhotoService
{

    Task<IEnumerable<Photo>> GetListAsync();

    Task<Photo> GetByIdAsync(int id);
}
