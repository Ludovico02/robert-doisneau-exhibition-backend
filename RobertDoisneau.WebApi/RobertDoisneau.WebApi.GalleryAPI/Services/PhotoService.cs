namespace RobertDoisneau.WebApi.GalleryAPI.Services;
using RobertDoisneau.WebApi.GalleryAPI.Models;
using Dapper;
using Npgsql;

public class PhotoService : IPhotoService
{

    private readonly string? _connectionString;
    private readonly ILogger<PhotoService> _logger;

    public PhotoService(IConfiguration configuration, ILogger<PhotoService> logger)
    {
        _connectionString = configuration.GetConnectionString("db");
        _logger = logger;

    }


    public async Task<Photo> GetByIdAsync(int id)
    {
        using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        string selectionQuery = """
            SELECT 
                id as Id,
                title_ita as TitleIta,
                title_eng as TitleEng,
                site as Site,
                date as Date,
                description as Description,
                alt_image as AltImage,
                url_image as UrlImage
            FROM 
                public.gallery
            WHERE 
                id = @PhotoId
            ORDER BY title_ita ASC
            
            """;

        var photo = await connection.QuerySingleOrDefaultAsync<Photo>(
                                                            selectionQuery,
                                                            new { PhotoId = id });

        return photo;
    }

    public async Task<IEnumerable<Photo>> GetListAsync()
    {
        using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

       
        string baseUrl = "/Img Foto Galleria/";

        string selectionQuery = """
            SELECT 
            id as Id,
            title_ita as TitleIta,
            title_eng as TitleEng,
            site as Site,
            date as Date,
            description as Description,
            alt_image as AltImage,
            url_image as UrlImage
            FROM public.gallery
            ORDER BY title_ita ASC
            """;

        var photos = (await connection.QueryAsync<Photo>(selectionQuery)).ToList();

        foreach (var p in photos)
        {
            p.UrlImage = baseUrl + p.UrlImage;
        }

        return photos;
    }
}
