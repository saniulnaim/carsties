using Mapster;
using Meilisearch;
using SearchService.Data;
using Wolverine;
using Wolverine.RabbitMQ;

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

builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMq(rabbit =>
    {
        rabbit.HostName = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        rabbit.UserName = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        rabbit.Password = builder.Configuration["RabbitMQ:Password"] ?? "guest";
    })
    .DeclareExchange("auction-created", ex => ex.ExchangeType = ExchangeType.Fanout)
    .BindExchange("auction-created").ToQueue("search-auction-created")
    .AutoProvision();

    opts.ListenToRabbitQueue("search-auction-created");
});

var app = builder.Build();

// Configure the HTTP request pipeline.

try
{
    await DbInitializer.InitDb(app);

}
catch (Exception ex)
{
    Console.WriteLine($"Error initializing the database: {ex.Message}");
}

app.Run();