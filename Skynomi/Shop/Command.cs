using Skynomi.Modules;
using Terraria;
using TShockAPI;

namespace Skynomi.Shop
{
    public static class Commands
    {
        public static void Initialize()
        {
            // Init Commands
            var cmds = new[]
            {
                new
                {
                    Perm = Permissions.ShopBuy,
                    Handler = (CommandDelegate)ShopBuy,
                    AllowServer = false,
                    Help = "Buy an item",
                    Names = new[] { "shopbuy", "sbuy" }
                },
                new
                {
                    Perm = Permissions.ShopSell,
                    Handler = (CommandDelegate)ShopSell,
                    AllowServer = false,
                    Help = "Sell an item",
                    Names = new[] { "shopsell", "ssell" }
                },
                new
                {
                    Perm = Permissions.ShopList,
                    Handler = (CommandDelegate)ShopList,
                    AllowServer = true,
                    Help = "List all items in the shop",
                    Names = new[] { "shoplist", "slist" }
                }
            };

            foreach (var c in cmds)
                TShockAPI.Commands.ChatCommands.Add(
                    new Command(c.Perm, c.Handler, c.Names)
                    {
                        AllowServer = c.AllowServer,
                        HelpText = c.Help
                    });
        }

        private static void ShopBuy(CommandArgs args)
        {
            var shopModule = ModuleManager.Get<Shop>();
            var economyModule = ModuleManager.Get<Economy.EconomyModule>();
            var utilsModule = ModuleManager.Get<Utils.UtilsModule>();

            if (!CheckRegion(args.Player, shopModule.ShopConfig.ShopRegion)) return;

            try
            {
                string usage = "Usage: /shop buy <item> [amount]";

                int itemAmount = 1;
                switch (args.Parameters.Count)
                {
                    case < 2:
                        args.Player.SendErrorMessage(usage);
                        return;
                    case >= 3 when !int.TryParse(args.Parameters[2], out itemAmount):
                        args.Player.SendErrorMessage("Invalid amount");
                        return;
                }

                var item = TShock.Utils.GetItemByIdOrName(args.Parameters[1]).FirstOrDefault(x => string.Equals(x.Name, args.Parameters[1], StringComparison.CurrentCultureIgnoreCase) ||
                    int.TryParse(args.Parameters[1], out int parsedId) && x.type == parsedId); // x.type or x.netID
                
                if (item == null)
                {
                    args.Player.SendErrorMessage("Item not found!");
                    return;
                }

                if (itemAmount < 1)
                {
                    args.Player.SendErrorMessage("Amount must be greater than 0.");
                    return;
                }

                // Check Item
                var isThereAny = false;
                var itemKey = "1";
                var itemValue = 0;
                var itemPrefix = 0;
                
                foreach (var i in shopModule.ShopConfig.ShopItems.Where(it => item.type.ToString() == it.Key)) // x.type or x.netID
                {
                    itemKey = i.Key;
                    itemValue = i.Value.buyPrice;
                    isThereAny = true;
                    itemPrefix = i.Value.prefix;
                    break;
                }

                // check item
                if (!isThereAny)
                {
                    args.Player.SendErrorMessage("Item not found in shop");
                    return;
                }

                // check balance
                var balance = economyModule.Db.GetWalletBalance(args.Player.Name);
                var itemId = int.Parse(itemKey);

                if (TShock.Utils.GetItemById(int.Parse(itemKey)).maxStack == 1)
                {
                    if (itemAmount > 1)
                    {
                        args.Player.SendErrorMessage("This item can only be bought one at a time.");
                        return;
                    }
                }

                long totalPrice = itemValue * itemAmount;
                if (balance < totalPrice)
                {
                    args.Player.SendErrorMessage(
                        $"You do not have enough {SkynomiPlugin.SkynomiConfig.Currency} to buy this item. (Need {utilsModule.CurrencyFormat(totalPrice - balance.Value)} more)");
                    return;
                }

                var itemCanUseThePrefix = TShock.Utils.GetItemById(itemId).Prefix(itemPrefix);
                if (!itemCanUseThePrefix)
                    itemPrefix = 0;

                args.Player.SendInfoMessage(
                    $"You have bought [i/s{itemAmount},p{itemPrefix}:{args.Parameters[1]}] for {utilsModule.CurrencyFormat(totalPrice)}");
                args.Player.GiveItem(itemId, itemAmount, itemPrefix);
                
                economyModule.Db.UpdateWalletBalance(args.Player.Account.Name, e =>
                {
                    e.Balance -= totalPrice;
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        private static void ShopSell(CommandArgs args)
        {
            var shopModule = ModuleManager.Get<Shop>();
            var economyModule = ModuleManager.Get<Economy.EconomyModule>();
            var utilsModule = ModuleManager.Get<Utils.UtilsModule>();

            if (!CheckRegion(args.Player, shopModule.ShopConfig.ShopRegion)) return;
            
            string usage = "Usage: /shop sell <item> [amount]";

            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage(usage);
                return;
            }

            var item = TShock.Utils.GetItemByIdOrName(args.Parameters[1]).FirstOrDefault(x => string.Equals(x.Name, args.Parameters[1], StringComparison.CurrentCultureIgnoreCase) ||
                int.TryParse(args.Parameters[1], out int parsedId) && x.type == parsedId); // x.type or x.netID
            
            if (item == null)
            {
                args.Player.SendErrorMessage("Item not found!");
                return;
            }

            int amount = 1;
            if (args.Parameters.Count > 2 && !int.TryParse(args.Parameters[2], out amount))
            {
                args.Player.SendErrorMessage("Invalid amount");
                return;
            }

            if (amount <= 0)
            {
                args.Player.SendErrorMessage("Amount must be greater than 0.");
                return;
            }

            // Check Item
            bool isThereAny = false;
            int itemValue = 0;
            
            foreach (var i in shopModule.ShopConfig.ShopItems.Where(i => item.type.ToString() == i.Key)) // x.type or x.netID
            {
                itemValue = i.Value.sellPrice;
                isThereAny = true;
                break;
            }

            // check item
            if (!isThereAny)
            {
                args.Player.SendErrorMessage("Item not sellable");
                return;
            }

            int totalOwned = args.Player.TPlayer.inventory.Where(i => i.type == item.type).Sum(_ => item.stack); // x.type or x.netID

            if (totalOwned < amount)
            {
                args.Player.SendErrorMessage($"You don't have {amount} of this item.");
                return;
            }

            if (!Main.ServerSideCharacter)
            {
                Main.ServerSideCharacter = true;
                NetMessage.SendData(7, args.Player.Index);
                args.Player.IgnoreSSCPackets = true;
            }

            int remainingToRemove = amount;
            for (int i = 0; i < args.Player.TPlayer.inventory.Length; i++)
            {
                if (args.Player.TPlayer.inventory[i].type != item.type) continue; // x.type or x.netID
                
                if (args.Player.TPlayer.inventory[i].stack > remainingToRemove)
                {
                    args.Player.TPlayer.inventory[i].stack -= remainingToRemove;
                    NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, null, args.Player.Index, i);
                    break;
                }

                remainingToRemove -= args.Player.TPlayer.inventory[i].stack;
                args.Player.TPlayer.inventory[i].netDefaults(0);
                NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, null, args.Player.Index, i);
            }

            long totalPrice = itemValue * amount;
            args.Player.SendInfoMessage(
                $"You have sell [i/s{amount}:{item.type}] for {utilsModule.CurrencyFormat((int)totalPrice)}"); // x.type or x.netID

            economyModule.Db.UpdateWalletBalance(args.Player.Account.Name, e =>
            {
                e.Balance += totalPrice;
            });
        }

        private static void ShopList(CommandArgs args)
        {
            var shopModule = ModuleManager.Get<Shop>();
            var utilsModule = ModuleManager.Get<Utils.UtilsModule>();
            
            if (args.Player.Active && !CheckRegion(args.Player, shopModule.ShopConfig.ShopRegion)) return;
            
            if (!utilsModule.CheckPermission(Permissions.ShopList, args)) return;

                int pageSize = shopModule.ShopConfig.ListLength;
                int currentPage = 1;
                int totalPages = (int)Math.Ceiling(shopModule.ShopConfig.ShopItems.Count / (double)pageSize);

                if (args.Parameters.Count > 1 && int.TryParse(args.Parameters[1], out int parsedPage))
                {
                    if (totalPages == 0)
                    {
                        args.Player.SendErrorMessage("No items available");
                        return;
                    }

                    currentPage = Math.Clamp(parsedPage, 1, totalPages);
                }

                var itemsToDisplay = shopModule.ShopConfig.ShopItems
                    .Skip((currentPage - 1) * pageSize)
                    .Take(pageSize);

                string message = $"Shop Items (Page {currentPage}/{totalPages})";
                int index = (currentPage - 1) * pageSize + 1;
                foreach (var item in itemsToDisplay)
                {
                    int itemId = Convert.ToInt32(item.Key);
                    int prefix = 0;
                    string prefixName = "";
                    if (itemId != 0)
                    {
                        bool itemCanUseThePrefix = TShock.Utils.GetItemById(itemId).Prefix(item.Value.prefix);
                        if (itemCanUseThePrefix)
                        {
                            prefix = item.Value.prefix;
                            prefixName = TShock.Utils.GetPrefixById(item.Value.prefix);
                        }
                    }

                    message +=
                        $"\n{index}. [i/p{prefix}:{item.Key}] {TShock.Utils.GetItemById(itemId).Name} ({item.Key}) {(!string.IsNullOrWhiteSpace(prefixName) ? "[" + prefixName + "] " : "")}- B: {utilsModule.CurrencyFormat(item.Value.buyPrice)} | S: {utilsModule.CurrencyFormat(item.Value.sellPrice)}";
                    index++;
                }


                args.Player.SendInfoMessage(message);
        }

        private static bool CheckRegion(TSPlayer plr, string shopRegion)
        {
            bool protectedByRegion = !string.IsNullOrEmpty(shopRegion);
            if (!protectedByRegion) return true;

            var region = TShock.Regions.GetRegionByName(shopRegion);
            if (region != null && region.InArea((int)(plr.X / 16), (int)(plr.Y / 16))) return true;

            plr.SendErrorMessage("You can only use the shop command in the shop region.");
            return false;
        }
    }
}