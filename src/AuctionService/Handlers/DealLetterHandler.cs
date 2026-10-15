using Contracts;

namespace AuctionService.Handlers
{
    public class DealLetterHandler(ILogger<DealLetterHandler> logger)
    {
        public Task Handle(AuctionCreated message)
        {
            logger.LogError("Deal letter handler triggered for {AuctionId} {Make} {Model}", message.Id, message.Make, message.Model);

            return Task.CompletedTask;
        }
    }
}
