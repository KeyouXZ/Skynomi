using Skynomi.Modules;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace Skynomi.Auction
{
    public class AuctionManager
    {
        private static List<AuctionItem> _activeAuctions = new();
        private static readonly object _lock = new();
        private static Config _config = null!;
        private static Database _db = null!;
        public static Dictionary<string, long> AwaitingDrops = new();
        private static DateTime _lastUpdate = DateTime.UtcNow;

        public static void Initialize(Config config)
        {
            _config = config;
            _db = new Database();

            _activeAuctions = _db.LoadActiveAuctions();

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            List<AuctionItem> toEnd = new();

            foreach (var auction in _activeAuctions)
            {
                if (auction.EndTime <= now)
                {
                    toEnd.Add(auction);
                }
            }

            foreach (var auction in toEnd)
            {
                EndAuction(auction);
            }

            ServerApi.Hooks.GameUpdate.Register(SkynomiPlugin.Instance, OnGameUpdate);
            ServerApi.Hooks.NetGetData.Register(SkynomiPlugin.Instance, OnGetData);
            ServerApi.Hooks.NetGreetPlayer.Register(SkynomiPlugin.Instance, OnPlayerLogin);
        }

        public static void Dispose()
        {
            ServerApi.Hooks.GameUpdate.Deregister(SkynomiPlugin.Instance, OnGameUpdate);
            ServerApi.Hooks.NetGetData.Deregister(SkynomiPlugin.Instance, OnGetData);
            ServerApi.Hooks.NetGreetPlayer.Deregister(SkynomiPlugin.Instance, OnPlayerLogin);
        }

        public static void Reload(Config config)
        {
            _config = config;
        }

        private static void OnGetData(GetDataEventArgs args)
        {
            if (args.MsgID == PacketTypes.ItemDrop)
            {
                var player = TShock.Players[args.Msg.whoAmI];
                if (player == null || !player.Active) return;

                if (AwaitingDrops.ContainsKey(player.Name))
                {
                    using (var reader = new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
                    {
                        short id = reader.ReadInt16();
                        float posX = reader.ReadSingle();
                        float posY = reader.ReadSingle();
                        float velX = reader.ReadSingle();
                        float velY = reader.ReadSingle();
                        short stack = reader.ReadInt16();
                        byte prefix = reader.ReadByte();
                        bool noDelay = reader.ReadBoolean();
                        short netId = reader.ReadInt16();

                        if (netId == 0) return;

                        long price = AwaitingDrops[player.Name];
                        AwaitingDrops.Remove(player.Name);

                        StartAuction(player.Name, netId, stack, prefix, price);

                        args.Handled = true;

                        var utils = ModuleManager.Get<Utils.UtilsModule>();
                        player.SendSuccessMessage($"Auction started for [i/s{stack}:{netId}] at {utils.CurrencyFormat(price)}!");
                    }
                }
            }
        }

        public static void StartAuction(string seller, int itemId, int stack, int prefix, long price)
        {
            long endTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + _config.AuctionDurationSeconds;
            int id = _db.CreateAuction(seller, itemId, stack, prefix, price, endTime);

            var auction = new AuctionItem
            {
                Id = id,
                SellerName = seller,
                ItemId = itemId,
                Stack = stack,
                Prefix = prefix,
                StartingPrice = price,
                CurrentBid = price,
                EndTime = endTime,
                IsActive = true
            };

            lock (_lock)
            {
                _activeAuctions.Add(auction);
            }

            if (_config.BroadcastAuction)
            {
                var utils = ModuleManager.Get<Utils.UtilsModule>();
                TSPlayer.All.SendInfoMessage($"[Auction] {seller} is auctioning [i/s{stack}:{itemId}] for {utils.CurrencyFormat(price)}! Type /auction bid {price + _config.MinBidIncrement} to bid.");
            }
        }

        public static void Bid(TSPlayer player, long amount)
        {
            AuctionItem? auction = null;
            lock (_lock)
            {
                auction = _activeAuctions.OrderByDescending(a => a.Id).FirstOrDefault();
            }

            var utils = ModuleManager.Get<Utils.UtilsModule>();
            var economy = ModuleManager.Get<Economy.EconomyModule>();

            if (auction == null)
            {
                player.SendErrorMessage("No active auctions.");
                return;
            }

            if (amount < auction.CurrentBid + _config.MinBidIncrement)
            {
                player.SendErrorMessage($"Bid must be at least {utils.CurrencyFormat(auction.CurrentBid + _config.MinBidIncrement)}.");
                return;
            }

            if (auction.SellerName == player.Name)
            {
                player.SendErrorMessage("You cannot bid on your own auction.");
                return;
            }

            if (auction.HighBidderName == player.Name)
            {
                player.SendErrorMessage("You are already the highest bidder.");
                return;
            }

            long balance = economy.Db.GetWalletBalance(player.Account.Name) ?? 0;
            if (balance < amount)
            {
                player.SendErrorMessage($"Not enough currency. You need {utils.CurrencyFormat(amount)}.");
                return;
            }

            if (auction.HighBidderName != null)
            {
                economy.Db.UpdateWalletBalance(auction.HighBidderName, w => w.Balance += auction.CurrentBid);
                var prevPlayer = TShock.Players.FirstOrDefault(p => p != null && p.Name == auction.HighBidderName);
                prevPlayer?.SendInfoMessage($"You have been outbid! {utils.CurrencyFormat(auction.CurrentBid)} refunded.");
            }

            economy.Db.UpdateWalletBalance(player.Account.Name, w => w.Balance -= amount);

            long newEndTime = auction.EndTime;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (now > auction.EndTime - _config.BidExtensionSeconds)
            {
                newEndTime += _config.BidExtensionSeconds;
            }

            auction.HighBidderName = player.Account.Name;
            auction.CurrentBid = amount;
            auction.EndTime = newEndTime;

            _db.UpdateBid(auction.Id, player.Account.Name, amount, newEndTime);

            TSPlayer.All.SendInfoMessage($"[Auction] {player.Name} bid {utils.CurrencyFormat(amount)} on [i/s{auction.Stack}:{auction.ItemId}]!");
        }

        public static void CancelAuction(TSPlayer player, int id = -1)
        {
            AuctionItem? auction = null;
            lock (_lock)
            {
                if (id == -1)
                    auction = _activeAuctions.FirstOrDefault(a => a.SellerName == player.Account.Name);
                else
                    auction = _activeAuctions.FirstOrDefault(a => a.Id == id);
            }

            if (auction == null)
            {
                player.SendErrorMessage("Auction not found.");
                return;
            }

            if (auction.SellerName != player.Account.Name && !player.HasPermission(Permissions.Admin))
            {
                player.SendErrorMessage("You can only cancel your own auctions.");
                return;
            }

            var economy = ModuleManager.Get<Economy.EconomyModule>();
            var utils = ModuleManager.Get<Utils.UtilsModule>();

            if (auction.HighBidderName != null)
            {
                economy.Db.UpdateWalletBalance(auction.HighBidderName, w => w.Balance += auction.CurrentBid);
                var prevPlayer = TShock.Players.FirstOrDefault(p => p != null && p.Name == auction.HighBidderName);
                prevPlayer?.SendInfoMessage($"Auction cancelled. {utils.CurrencyFormat(auction.CurrentBid)} refunded.");
            }

            DeliverItem(auction.SellerName, auction.ItemId, auction.Stack, auction.Prefix);

            _db.EndAuction(auction.Id);
            lock (_lock)
            {
                _activeAuctions.Remove(auction);
            }
            player.SendSuccessMessage("Auction cancelled.");
        }

        public static void ForceEndAuction(TSPlayer player, int id)
        {
            AuctionItem? auction = null;
            lock (_lock)
            {
                auction = _activeAuctions.FirstOrDefault(a => a.Id == id);
            }

            if (auction == null)
            {
                player.SendErrorMessage("Auction not found.");
                return;
            }

            _db.EndAuction(auction.Id);
            lock (_lock)
            {
                _activeAuctions.Remove(auction);
            }

            var economy = ModuleManager.Get<Economy.EconomyModule>();
            var utils = ModuleManager.Get<Utils.UtilsModule>();

            if (auction.HighBidderName != null)
            {
                DeliverItem(auction.HighBidderName, auction.ItemId, auction.Stack, auction.Prefix);

                economy.Db.UpdateWalletBalance(auction.SellerName, w => w.Balance += auction.CurrentBid);

                TSPlayer.All.SendInfoMessage($"[Auction] Auction force ended! {auction.HighBidderName} won [i/s{auction.Stack}:{auction.ItemId}] for {utils.CurrencyFormat(auction.CurrentBid)}.");

                var seller = TShock.Players.FirstOrDefault(p => p != null && p.Account?.Name == auction.SellerName);
                seller?.SendSuccessMessage($"Auction sold! You received {utils.CurrencyFormat(auction.CurrentBid)}.");
            }
            else
            {
                DeliverItem(auction.SellerName, auction.ItemId, auction.Stack, auction.Prefix);

                var seller = TShock.Players.FirstOrDefault(p => p != null && p.Account?.Name == auction.SellerName);
                seller?.SendErrorMessage("Auction ended with no bids. Item returned.");
            }

            player.SendSuccessMessage($"Auction #{id} force ended.");
        }

        private static void OnGameUpdate(EventArgs args)
        {
            if ((DateTime.UtcNow - _lastUpdate).TotalSeconds < 1) return;
            _lastUpdate = DateTime.UtcNow;

            List<AuctionItem> toEnd = new();
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            lock (_lock)
            {
                foreach (var auction in _activeAuctions)
                {
                    if (auction.EndTime <= now)
                    {
                        toEnd.Add(auction);
                    }
                }
            }

            foreach (var auction in toEnd)
            {
                EndAuction(auction);
            }
        }

        private static void EndAuction(AuctionItem auction)
        {
            _db.EndAuction(auction.Id);
            lock (_lock)
            {
                _activeAuctions.Remove(auction);
            }

            var economy = ModuleManager.Get<Economy.EconomyModule>();
            var utils = ModuleManager.Get<Utils.UtilsModule>();

            if (auction.HighBidderName != null)
            {
                DeliverItem(auction.HighBidderName, auction.ItemId, auction.Stack, auction.Prefix);

                economy.Db.UpdateWalletBalance(auction.SellerName, w => w.Balance += auction.CurrentBid);

                TSPlayer.All.SendInfoMessage($"[Auction] Auction ended! {auction.HighBidderName} won [i/s{auction.Stack}:{auction.ItemId}] for {utils.CurrencyFormat(auction.CurrentBid)}.");

                var seller = TShock.Players.FirstOrDefault(p => p != null && p.Account?.Name == auction.SellerName);
                seller?.SendSuccessMessage($"Auction sold! You received {utils.CurrencyFormat(auction.CurrentBid)}.");
            }
            else
            {
                DeliverItem(auction.SellerName, auction.ItemId, auction.Stack, auction.Prefix);

                var seller = TShock.Players.FirstOrDefault(p => p != null && p.Account?.Name == auction.SellerName);
                seller?.SendErrorMessage("Auction ended with no bids. Item returned.");
            }
        }

        public static void DeliverItem(string playerName, int itemId, int stack, int prefix)
        {
            var player = TShock.Players.FirstOrDefault(p => p != null && p.Account?.Name == playerName);
            if (player != null && player.Active)
            {
                bool inventoryFull = true;
                for (int i = 0; i < 50; i++)
                {
                    if (player.TPlayer.inventory[i].type == 0 || (player.TPlayer.inventory[i].type == itemId && player.TPlayer.inventory[i].stack < player.TPlayer.inventory[i].maxStack))
                    {
                        inventoryFull = false;
                        break;
                    }
                }

                if (inventoryFull)
                {
                    _db.AddToDeliveryQueue(playerName, itemId, stack, prefix);
                    player.SendErrorMessage("Inventory full! Item added to delivery queue. Use /auction claim.");
                }
                else
                {
                    player.GiveItem(itemId, stack, prefix);
                    player.SendSuccessMessage($"Received [i/s{stack}:{itemId}].");
                }
            }
            else
            {
                _db.AddToDeliveryQueue(playerName, itemId, stack, prefix);
            }
        }

        public static void Claim(TSPlayer player)
        {
            var items = _db.GetDeliveries(player.Account.Name);
            if (items.Count == 0)
            {
                player.SendInfoMessage("No items to claim.");
                return;
            }

            foreach (var item in items)
            {
                bool inventoryFull = true;
                for (int i = 0; i < 50; i++)
                {
                    if (player.TPlayer.inventory[i].type == 0 || (player.TPlayer.inventory[i].type == item.ItemId && player.TPlayer.inventory[i].stack < player.TPlayer.inventory[i].maxStack))
                    {
                        inventoryFull = false;
                        break;
                    }
                }

                if (!inventoryFull)
                {
                    player.GiveItem(item.ItemId, item.Stack, item.Prefix);
                    _db.RemoveDelivery(item.Id);
                }
                else
                {
                    player.SendErrorMessage("Inventory full. Clear space and try again.");
                    break;
                }
            }
            player.SendSuccessMessage("Claimed items.");
        }

        private static void OnPlayerLogin(GreetPlayerEventArgs args)
        {
            var player = TShock.Players[args.Who];
            if (player == null || player.Account == null) return;

            var deliveries = _db.GetDeliveries(player.Account.Name);
            if (deliveries.Count > 0)
            {
                player.SendInfoMessage($"You have {deliveries.Count} items in your delivery queue! Type /auction claim to receive them.");
            }
        }

        public static List<AuctionItem> GetActiveAuctions()
        {
            lock (_lock)
            {
                return _activeAuctions.ToList();
            }
        }

        public static AuctionItem? GetAuction(int id)
        {
            lock (_lock)
            {
                return _activeAuctions.FirstOrDefault(a => a.Id == id);
            }
        }
    }
}
