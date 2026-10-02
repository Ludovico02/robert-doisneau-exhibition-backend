namespace RobertDoisneau.Cart.V2.WebApi.Models;

public class CheckoutItem
{
    /// <summary>Id of the exhibition (legacy name kept because the frontend sends <c>ticketCategoryId</c>).</summary>
    public int TicketCategoryId { get; set; }
    public int Quantity { get; set; }
}
