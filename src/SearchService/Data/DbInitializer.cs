using Meilisearch;
using SearchService.Models;
using SearchService.Services;
using System.Text.Json;

namespace SearchService.Data
{
    public class DbInitializer
    {
        private const string IndexUid = "items";

        public static async Task InitDb(WebApplication app)
        {
            //var client = app.Services.GetRequiredService<MeilisearchClient>();

            //if (await IndexHasDocuments(client))
            //{
            //    Console.WriteLine("Search index already has documents. Seeding skipped");
            //    return;
            //}

            //var path = Path.Combine(AppContext.BaseDirectory, "Data", "auctions.json");
            //await using var stream = File.OpenRead(path);
            //var options = new JsonSerializerOptions
            //{
            //    PropertyNameCaseInsensitive = true
            //};
            //var items = await JsonSerializer.DeserializeAsync<List<Item>>(stream, options);

            //if(items?.Count == 0)
            //{
            //    Console.WriteLine("No items found in the JSON file. Seeding skipped");
            //    return;
            //}

            //var index = client.Index(IndexUid);
            //var addTask = await index.AddDocumentsAsync(items, primaryKey: "id");
            //await client.WaitForTaskAsync(addTask.TaskUid);

            //var settingsTask = await index.UpdateSettingsAsync(new Settings
            //{
            //    SearchableAttributes = [ "make", "model", "description" ],
            //    FilterableAttributes = [ "seller", "winner", "status", "auctionEnd" ],
            //    SortableAttributes =  [ "auctionEnd", "currentHighBid", "createdAt", "updatedAt", "make", "model" ]
            //});
            //await client.WaitForTaskAsync(settingsTask.TaskUid);

            //Console.WriteLine($"Search index populated with {items?.Count} items");

            var client = app.Services.GetRequiredService<MeilisearchClient>();
            var index = client.Index(IndexUid);

            var settingsTask = await index.UpdateSettingsAsync(new Settings
            {
                SearchableAttributes = ["make", "model", "description"],
                FilterableAttributes = ["seller", "winner", "status", "auctionEnd"],
                SortableAttributes = ["auctionEnd", "currentHighBid", "createdAt", "updatedAt", "make", "model"]
            });
            await client.WaitForTaskAsync(settingsTask.TaskUid);

            using var scope = app.Services.CreateScope();
            var auctionSvc = scope.ServiceProvider.GetRequiredService<AuctionSvcHttpClient>();

            var items = await auctionSvc.GetItemsForSearch();
            if (items.Count == 0)
            {
                Console.WriteLine("Search index is up to date with the auctionservice");
                return;
            }


            var addTask = await index.AddDocumentsAsync(items, primaryKey: "id");
            await client.WaitForTaskAsync(addTask.TaskUid);
        }

        public static async Task<bool> IndexHasDocuments(MeilisearchClient client)
        {
            var indexes = await client.GetAllIndexesAsync();
            if (indexes.Results.All(i => i.Uid != IndexUid))
                return false;

            var starts = await client.Index(IndexUid).GetStatsAsync();
            return starts.NumberOfDocuments > 0;
        }
    }
}
