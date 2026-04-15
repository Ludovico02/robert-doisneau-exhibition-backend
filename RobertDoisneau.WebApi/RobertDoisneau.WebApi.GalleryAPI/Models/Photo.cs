using System.Text.Json.Serialization;

namespace RobertDoisneau.WebApi.GalleryAPI.Models;

public class Photo
{
    public int Id { get; set; }

    //Titolo in italiano
    public string TitleIta { get; set; } = default!;

    //Titolo in inglese
    public string TitleEng { get; set; } = default!;
    //Luogo in cui è stata scattata la foto
    public string? Site { get; set; }
    //Anno in cui è stata scattata la foto
    public string? Date { get; set; }
    //Descrizione della foto
    public string? Description{ get; set; }
    //L' ALT dell'immagine
    public string? AltImage { get; set; }
    //URL dell'immagine
    public string UrlImage { get; set;} = default!;
}
