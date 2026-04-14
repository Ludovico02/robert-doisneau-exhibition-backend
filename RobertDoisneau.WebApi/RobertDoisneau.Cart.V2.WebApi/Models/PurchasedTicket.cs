using System.ComponentModel.DataAnnotations;

namespace RobertDoisneau.Cart.V2.WebApi.Models;

public class PurchasedTicket
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int TicketCategoryId { get; set; }

    [Required]
    [StringLength(100)]
    public string UniqueCode { get; set; } = string.Empty;

    [Required]
    //[Column("price_paid", TypeName = "decimal(10,2)")]
    public decimal PricePaid { get; set; }

    public DateTime PurchaseDate { get; set; }
}
