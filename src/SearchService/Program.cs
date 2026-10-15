using Mapster;
using Meilisearch;
using SearchService.Data;
using Wolverine;
using Wolverine.RabbitMQ;
using SearchService.Endpoints;
using SearchService.Models;
using SearchService.Services;
using Wolverine.ErrorHandling;
using SearchService.Handlers;

// Mapster
TypeAdapterConfig.GlobalSettings.Scan(typeof(Program).Assembly);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();

    return new MeilisearchClient(
        config["Meilisearch:Url"],
        config["Meilisearch:ApiKey"]);
});

builder.Services.AddHttpClient<AuctionSvcHttpClient>()
    .AddStandardResilienceHandler(options =>
    {
        options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);
        options.Retry.MaxRetryAttempts = 5;
        options.Retry.Delay = TimeSpan.FromSeconds(10);
        options.Retry.OnRetry = orgs =>
        {
            Console.WriteLine("Auctions vc unavailable");
            return default;
        };
    });

builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMq(rabbit =>
    {
        rabbit.HostName = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        rabbit.UserName = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        rabbit.Password = builder.Configuration["RabbitMQ:Password"] ?? "guest";
    })
    .DeclareExchange("auction-created", ex => ex.ExchangeType = ExchangeType.Fanout)
    .DeclareExchange("auction-updated", ex => ex.ExchangeType = ExchangeType.Fanout)
    .DeclareExchange("auction-deleted", ex => ex.ExchangeType = ExchangeType.Fanout)
    .BindExchange("auction-created").ToQueue("search-auction-created")
    .BindExchange("auction-updated").ToQueue("search-auction-updated")
    .BindExchange("auction-deleted").ToQueue("search-auction-deleted")
    .AutoProvision();

    opts.ListenToRabbitQueue("search-auction-created");
    opts.ListenToRabbitQueue("search-auction-updated");
    opts.ListenToRabbitQueue("search-auction-deleted");

    opts.OnException<TransientSearchException>()
        .RetryWithCooldown(
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromSeconds(1))
        .Then.MoveToErrorQueue();
});

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapGet("/api/search", SearchEndpoints.GetSearchResults);
app.MapGet("/api/search/{id}", SearchEndpoints.GetAuctionById);

try
{
    await DbInitializer.ConfigureIndex(app);

}
catch (Exception ex)
{
    Console.WriteLine($"Error initializing the database: {ex.Message}");
}

_ = Task.Run(async () =>
{
    try
    {
        await DbInitializer.FetchMissingAuctions(app);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Failed to seed search");
    }
});

app.Run();