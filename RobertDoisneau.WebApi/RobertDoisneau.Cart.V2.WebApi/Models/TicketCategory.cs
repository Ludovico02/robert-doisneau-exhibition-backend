using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RobertDoisneau.Cart.V2.WebApi.Models;

public class TicketCategory
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ExhibitionId { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    //[Column("price", TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [StringLength(255)]
    public string? Description { get; set; }

    [ForeignKey("ExhibitionId")]
    public virtual Exhibition? Exhibition { get; set; }
}
