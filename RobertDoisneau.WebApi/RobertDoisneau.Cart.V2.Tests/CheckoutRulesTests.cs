using RobertDoisneau.Cart.V2.WebApi.Models;
using RobertDoisneau.Cart.V2.WebApi.Services;
using Xunit;

namespace RobertDoisneau.Cart.V2.Tests;

public class CheckoutRulesTests
{
    [Fact]
    public void TryNormalize_RejectsNullOrEmptyCart()
    {
        Assert.False(CheckoutRules.TryNormalize(null, out _, out _));
        Assert.False(CheckoutRules.TryNormalize([], out _, out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(11)]
    public void TryNormalize_RejectsInvalidQuantity(int quantity)
    {
        Assert.False(
            CheckoutRules.TryNormalize([new CheckoutItem { TicketCategoryId = 1, Quantity = quantity }], out _, out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TryNormalize_RejectsInvalidExhibitionId(int exhibitionId)
    {
        Assert.False(
            CheckoutRules.TryNormalize([new CheckoutItem { TicketCategoryId = exhibitionId, Quantity = 1 }], out _, out _));
    }

    [Fact]
    public void TryNormalize_MergesDuplicateExhibitions()
    {
        var valid = CheckoutRules.TryNormalize(
            [
                new CheckoutItem { TicketCategoryId = 1, Quantity = 2 },
                new CheckoutItem { TicketCategoryId = 1, Quantity = 3 }
            ],
            out var normalized,
            out _);

        Assert.True(valid);
        var item = Assert.Single(normalized);
        Assert.Equal(1, item.TicketCategoryId);
        Assert.Equal(5, item.Quantity);
    }

    [Fact]
    public void TryNormalize_RejectsMergedQuantityAboveLimit()
    {
        Assert.False(
            CheckoutRules.TryNormalize(
                [
                    new CheckoutItem { TicketCategoryId = 1, Quantity = 6 },
                    new CheckoutItem { TicketCategoryId = 1, Quantity = 6 }
                ],
                out _,
                out _));
    }

    [Fact]
    public void TryNormalize_SortsByExhibitionId()
    {
        var valid = CheckoutRules.TryNormalize(
            [
                new CheckoutItem { TicketCategoryId = 3, Quantity = 1 },
                new CheckoutItem { TicketCategoryId = 1, Quantity = 1 }
            ],
            out var normalized,
            out _);

        Assert.True(valid);
        Assert.Equal([1, 3], normalized.Select(item => item.TicketCategoryId));
    }
}
