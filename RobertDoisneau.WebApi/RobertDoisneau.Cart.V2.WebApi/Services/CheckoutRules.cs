using RobertDoisneau.Cart.V2.WebApi.Models;

namespace RobertDoisneau.Cart.V2.WebApi.Services;

public static class CheckoutRules
{
    public const int MaxLinesInRequest = 100;
    public const int MaxDistinctExhibitionsPerOrder = 20;
    public const int MaxQuantityPerExhibition = 10;

    /// <summary>Validates the cart, merges duplicate exhibitions, and sorts by exhibition id.</summary>
    public static bool TryNormalize(
        IEnumerable<CheckoutItem>? items,
        out List<CheckoutItem> normalized,
        out string? error)
    {
        normalized = new List<CheckoutItem>();
        error = null;

        var lines = items?.ToList();
        if (lines is null || lines.Count == 0)
        {
            error = "The cart is empty or the request is not valid.";
            return false;
        }

        if (lines.Count > MaxLinesInRequest)
        {
            error = "The cart contains too many items.";
            return false;
        }

        foreach (var line in lines)
        {
            if (line is null || line.TicketCategoryId <= 0)
            {
                error = "The cart contains an invalid exhibition id.";
                return false;
            }

            if (line.Quantity < 1 || line.Quantity > MaxQuantityPerExhibition)
            {
                error = $"Quantity must be between 1 and {MaxQuantityPerExhibition} for each exhibition.";
                return false;
            }
        }

        var merged = lines
            .GroupBy(line => line.TicketCategoryId)
            .Select(group => new CheckoutItem
            {
                TicketCategoryId = group.Key,
                Quantity = group.Sum(line => line.Quantity)
            })
            .OrderBy(line => line.TicketCategoryId)
            .ToList();

        if (merged.Count > MaxDistinctExhibitionsPerOrder)
        {
            error = $"You can buy tickets for at most {MaxDistinctExhibitionsPerOrder} exhibitions at a time.";
            return false;
        }

        if (merged.Any(line => line.Quantity > MaxQuantityPerExhibition))
        {
            error = $"Quantity must be between 1 and {MaxQuantityPerExhibition} for each exhibition.";
            return false;
        }

        normalized = merged;
        return true;
    }
}
