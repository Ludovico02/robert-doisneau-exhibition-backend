using System.ComponentModel.DataAnnotations;

namespace RobertDoisneau.Cart.V2.WebApi.Models;

public class Exhibition
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? Description2 { get; set; }
    public string? Description3 { get; set; }

    public decimal? Price { get; set; }

    [Required]
    public int TotalCapacity { get; set; }

    [Required]
    public int Availability { get; set; }



}
