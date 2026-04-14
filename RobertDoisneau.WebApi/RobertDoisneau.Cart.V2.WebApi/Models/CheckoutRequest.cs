namespace RobertDoisneau.Cart.V2.WebApi.Models;

public class CheckoutRequest
{
    public int UserId { get; set; }
    public List<CheckoutItem>? Items { get; set; }
}
