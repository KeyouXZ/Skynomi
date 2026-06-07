using LiteDB;
using Skynomi.Modules;

namespace Skynomi.Auction
{
    public class Database
    {
        private ILiteDatabase Db => ModuleManager.Get<Skynomi.Database.DatabaseModule>().Db;

        public int CreateAuction(string seller, int itemId, int stack, int prefix, long price, long endTime)
        {
            var col = Db.GetCollection<AuctionItem>("Auctions");
            var auction = new AuctionItem
            {
                SellerName = seller,
                ItemId = itemId,
                Stack = stack,
                Prefix = prefix,
                StartingPrice = price,
                CurrentBid = price,
                EndTime = endTime,
                IsActive = true
            };
            var id = col.Insert(auction);
            return id.AsInt32;
        }

        public void UpdateBid(int auctionId, string bidder, long bidAmount, long newEndTime)
        {
            var col = Db.GetCollection<AuctionItem>("Auctions");
            var auction = col.FindById(auctionId);
            if (auction != null)
            {
                auction.HighBidderName = bidder;
                auction.CurrentBid = bidAmount;
                auction.EndTime = newEndTime;
                col.Update(auction);
            }
        }

        public void EndAuction(int auctionId)
        {
            var col = Db.GetCollection<AuctionItem>("Auctions");
            var auction = col.FindById(auctionId);
            if (auction != null)
            {
                auction.IsActive = false;
                col.Update(auction);
            }
        }

        public void AddToDeliveryQueue(string player, int itemId, int stack, int prefix, long money = 0)
        {
            var col = Db.GetCollection<DeliveryItem>("DeliveryQueue");
            var delivery = new DeliveryItem
            {
                PlayerName = player,
                ItemId = itemId,
                Stack = stack,
                Prefix = prefix,
                Money = money
            };
            col.Insert(delivery);
        }

        public List<AuctionItem> LoadActiveAuctions()
        {
            var col = Db.GetCollection<AuctionItem>("Auctions");
            return col.Find(x => x.IsActive).ToList();
        }

        public List<DeliveryItem> GetDeliveries(string player)
        {
            var col = Db.GetCollection<DeliveryItem>("DeliveryQueue");
            return col.Find(x => x.PlayerName == player).ToList();
        }

        public void RemoveDelivery(int id)
        {
            var col = Db.GetCollection<DeliveryItem>("DeliveryQueue");
            col.Delete(id);
        }
    }

    public class AuctionItem
    {
        public int Id { get; set; }
        public required string SellerName { get; set; }
        public int ItemId { get; set; }
        public int Stack { get; set; }
        public int Prefix { get; set; }
        public long StartingPrice { get; set; }
        public long CurrentBid { get; set; }
        public string? HighBidderName { get; set; }
        public long EndTime { get; set; }
        public bool IsActive { get; set; }
    }

    public class DeliveryItem
    {
        public int Id { get; set; }
        public required string PlayerName { get; set; }
        public int ItemId { get; set; }
        public int Stack { get; set; }
        public int Prefix { get; set; }
        public long Money { get; set; }
    }
}
