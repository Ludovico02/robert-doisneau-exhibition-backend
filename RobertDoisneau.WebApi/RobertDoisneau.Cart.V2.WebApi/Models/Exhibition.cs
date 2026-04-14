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

    [Required]
    public int TotalCapacity { get; set; }

    [Required]
    public int Availability { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public virtual List<TicketCategory> Categories { get; set; } = [];
}
