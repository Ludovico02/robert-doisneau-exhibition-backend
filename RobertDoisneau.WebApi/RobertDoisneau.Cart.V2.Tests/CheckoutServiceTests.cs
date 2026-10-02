using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using RobertDoisneau.Cart.V2.WebApi.Models;
using RobertDoisneau.Cart.V2.WebApi.Services;
using Xunit;

namespace RobertDoisneau.Cart.V2.Tests;

[Collection("db")]
public class CheckoutServiceTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Success_DecrementsStockAndCreatesDistinctTicketsAtDatabasePrice()
    {
        await SeedAsync((10, 12.00m));
        var result = await CreateService().ProcessPurchaseAsync(1, [Item(1, 3)]);

        Assert.Equal(PurchaseResult.Success, result);
        Assert.Equal(7, await GetAvailabilityAsync(1));
        var tickets = await GetTicketsAsync();
        Assert.Equal(3, tickets.Count);
        Assert.Equal(3, tickets.Select(ticket => ticket.UniqueCode).Distinct().Count());
        Assert.All(tickets, ticket => Assert.Equal(12.00m, ticket.PricePaid));
    }

    [Fact]
    public async Task NegativeQuantity_IsRejectedWithoutChangingDatabase()
    {
        await SeedAsync((10, 12.00m));
        var result = await CreateService().ProcessPurchaseAsync(1, [Item(1, -50)]);

        Assert.Equal(PurchaseResult.InvalidRequest, result);
        Assert.Equal(10, await GetAvailabilityAsync(1));
        Assert.Empty(await GetTicketsAsync());
    }

    [Fact]
    public async Task SoldOut_ReturnsSoldOutWithoutChangingDatabase()
    {
        await SeedAsync((2, 12.00m));
        var result = await CreateService().ProcessPurchaseAsync(1, [Item(1, 3)]);

        Assert.Equal(PurchaseResult.SoldOut, result);
        Assert.Equal(2, await GetAvailabilityAsync(1));
        Assert.Empty(await GetTicketsAsync());
    }

    [Fact]
    public async Task UnknownExhibition_ReturnsNotFound()
    {
        await SeedAsync();
        var result = await CreateService().ProcessPurchaseAsync(1, [Item(999, 1)]);

        Assert.Equal(PurchaseResult.NotFound, result);
    }

    [Fact]
    public async Task PartialFailure_RollsBackEarlierExhibitionPurchase()
    {
        await SeedAsync((10, 12.00m), (1, 20.00m));
        var result = await CreateService().ProcessPurchaseAsync(1, [Item(1, 2), Item(2, 2)]);

        Assert.Equal(PurchaseResult.SoldOut, result);
        Assert.Equal(10, await GetAvailabilityAsync(1));
        Assert.Empty(await GetTicketsAsync());
    }

    [Fact]
    public async Task ParallelBuyers_NeverOversell()
    {
        await SeedAsync((5, 12.00m));
        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ =>
            Task.Run(() => CreateService().ProcessPurchaseAsync(1, [Item(1, 1)]))));

        Assert.Equal(5, results.Count(result => result == PurchaseResult.Success));
        Assert.Equal(15, results.Count(result => result == PurchaseResult.SoldOut));
        Assert.Equal(0, await GetAvailabilityAsync(1));
        Assert.Equal(5, (await GetTicketsAsync()).Count);
    }

    [Fact]
    public async Task OppositeOrderCarts_CompleteWithoutDeadlock()
    {
        await SeedAsync((1000, 12.00m), (1000, 20.00m));
        var results = await Task.WhenAll(Enumerable.Range(0, 40).Select(index =>
        {
            var order = index % 2 == 0
                ? new[] { Item(1, 1), Item(2, 1) }
                : new[] { Item(2, 1), Item(1, 1) };

            return Task.Run(() => CreateService().ProcessPurchaseAsync(1, order));
        }));

        Assert.All(results, result => Assert.Equal(PurchaseResult.Success, result));
        Assert.Equal(960, await GetAvailabilityAsync(1));
        Assert.Equal(960, await GetAvailabilityAsync(2));
    }

    private CheckoutService CreateService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:db"] = fixture.ConnectionString
            })
            .Build();

        return new CheckoutService(configuration, NullLogger<CheckoutService>.Instance);
    }

    private async Task SeedAsync(params (int Capacity, decimal Price)[] exhibitions)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await connection.ExecuteAsync(
            "TRUNCATE public.purchased_tickets, public.exhibitions, public.users RESTART IDENTITY CASCADE;");
        await connection.ExecuteAsync(
            "INSERT INTO public.users (username, password_hash, email) VALUES ('test-user', 'hash', 'test@example.com');");

        foreach (var (capacity, price) in exhibitions)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO public.exhibitions (title, price, total_capacity, availability)
                VALUES (@Title, @Price, @Capacity, @Capacity);
                """,
                new { Title = $"Exhibition {capacity} {price}", Price = price, Capacity = capacity });
        }
    }

    private async Task<int> GetAvailabilityAsync(int exhibitionId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        return await connection.ExecuteScalarAsync<int>(
            "SELECT availability FROM public.exhibitions WHERE id = @ExhibitionId;",
            new { ExhibitionId = exhibitionId });
    }

    private async Task<List<TicketRow>> GetTicketsAsync()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        var tickets = await connection.QueryAsync<TicketRow>(
            "SELECT unique_code AS UniqueCode, price_paid AS PricePaid FROM public.purchased_tickets;");
        return tickets.ToList();
    }

    private static CheckoutItem Item(int exhibitionId, int quantity) =>
        new() { TicketCategoryId = exhibitionId, Quantity = quantity };

    private sealed class TicketRow
    {
        public string UniqueCode { get; set; } = string.Empty;
        public decimal PricePaid { get; set; }
    }
}
