using System.ComponentModel.DataAnnotations;

namespace RobertDoisneau.Cart.WebApi.Models;

public class Cart
{
    [Key]
    public int Id {  get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int ExhibitionId { get; set; }

    [Required]
    public int Quantity { get; set; }

    [Required]
    public DateTime Date { get; set; } = DateTime.Now;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

}
