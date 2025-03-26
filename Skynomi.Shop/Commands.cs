using Terraria;
using TShockAPI;

namespace Skynomi.ShopSystem
{
    public static class Commands
    {
        private static Skynomi.Config config;
        private static Config shopConfig;
        private static readonly Skynomi.Database.Database database = new();

        public static void Initialize()
        {
            config = Skynomi.Config.Read();
            shopConfig = Config.Read();

            TShockAPI.Commands.ChatCommands.Add(new Command(Permissions.Shop, Shop, "shop")
            {
                AllowServer = false,
                HelpText = "Shop commands:\nbuy <item> [amount] - Buy an item\nsell <item> [amount] - Sell an item\nlist [page] - List all items in the shop"
            });
        }

        public static void Reload()
        {
            config = Skynomi.Config.Read();
            shopConfig = Config.Read();
        }

        private static void Shop(CommandArgs args)
        {
            string shopUsage = "Usage: /shop <buy/sell/list>";
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage(shopUsage);
                return;
            }

            // Check if the player is in the allowed region
            if (shopConfig.ProtectedByRegion)
            {
                var region = TShock.Regions.GetRegionByName(shopConfig.ShopRegion);
                if (region == null || !region.InArea((int)(args.Player.X / 16), (int)(args.Player.Y / 16)))
                {
                    args.Player.SendErrorMessage("You can only use the shop command in the shop region.");
                    return;
                }
            }

            #region Buy
            if (args.Parameters[0] == "buy")
            {
                if (!Utils.Util.CheckPermission(Permissions.Buy, args)) return;

                try
                {
                    string usage = "Usage: /shop buy <item> [amount]";

                    int itemAmount = 1;
                    if (args.Parameters.Count < 2)
                    {
                        args.Player.SendErrorMessage(usage);
                        return;
                    }
                    else if (!int.TryParse(args.Parameters[1], out _))
                    {
                        args.Player.SendErrorMessage("Invalid item ID");
                        return;
                    }
                    else if (args.Parameters.Count >= 3 && !int.TryParse(args.Parameters[2], out itemAmount))
                    {
                        args.Player.SendErrorMessage("Invalid amount");
                        return;
                    }

                    if (itemAmount < 1)
                    {
                        args.Player.SendErrorMessage("Amount must be greater than 0.");
                        return;
                    }

                    // Check Item
                    bool isThereAny = false;
                    string itemKey = "1";
                    int itemValue = 0;
                    int itemPrefix = 0;
                    foreach (var item in shopConfig.ShopItems.Where(item => args.Parameters[1] == item.Key))
                    {
                        itemKey = item.Key;
                        itemValue = item.Value.buyPrice;
                        isThereAny = true;
                        itemPrefix = item.Value.prefix;
                        break;
                    }

                    // check item
                    if (!isThereAny)
                    {
                        args.Player.SendErrorMessage("Item not found");
                        return;
                    }

                    // check balance
                    long balance = database.GetBalance(args.Player.Name);
                    int itemId = int.Parse(itemKey);

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
                        args.Player.SendErrorMessage($"You do not have enough {config.Currency} to buy this item. (Need {Utils.Util.CurrencyFormat(totalPrice - balance)} more)");
                        return;
                    }

                    bool itemCanUseThePrefix = TShock.Utils.GetItemById(itemId).CanApplyPrefix(itemPrefix);
                    if (!itemCanUseThePrefix)
                        itemPrefix = 0;

                    args.Player.SendInfoMessage($"You have bought [i/s{itemAmount},p{itemPrefix}:{args.Parameters[1]}] for {Utils.Util.CurrencyFormat(totalPrice)}");
                    args.Player.GiveItem(itemId, itemAmount, itemPrefix);
                    database.RemoveBalance(args.Player.Name, totalPrice);

                }
                catch (Exception ex)
                {
                    Utils.Log.Error(ex.ToString());
                }
            }
            #endregion
            #region Sell
            else if (args.Parameters[0] == "sell")
            {
                if (!Utils.Util.CheckPermission(Permissions.Sell, args)) return;

                string usage = "Usage: /shop sell <item> [amount]";

                if (args.Parameters.Count < 2)
                {
                    args.Player.SendErrorMessage(usage);
                    return;
                }

                if (!int.TryParse(args.Parameters[1], out var itemID))
                {
                    args.Player.SendErrorMessage("Invalid item ID");
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
                foreach (var item in shopConfig.ShopItems)
                {
                    if (itemID.ToString() == item.Key)
                    {
                        itemValue = item.Value.sellPrice;
                        isThereAny = true;
                        break;
                    }
                }

                // check item
                if (!isThereAny)
                {
                    args.Player.SendErrorMessage("Item not sellable");
                    return;
                }

                int totalOwned = 0;
                foreach (var item in args.Player.TPlayer.inventory)
                {
                    if (item.type == itemID)
                    {
                        totalOwned += item.stack;
                    }
                }

                if (totalOwned < amount)
                {
                    args.Player.SendErrorMessage($"You don't have {amount} of this item.");
                    return;
                }

                bool isSSC = Main.ServerSideCharacter;

                if (!isSSC)
                {
                    Main.ServerSideCharacter = true;
                    NetMessage.SendData(7, args.Player.Index);
                    args.Player.IgnoreSSCPackets = true;
                }

                int remainingToRemove = amount;
                for (int i = 0; i < args.Player.TPlayer.inventory.Length; i++)
                {
                    if (args.Player.TPlayer.inventory[i].type == itemID)
                    {
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
                }

                long totalPrice = itemValue * amount;
                args.Player.SendInfoMessage($"You have sell [i/s{amount}:{itemID}] for {Utils.Util.CurrencyFormat((int)totalPrice)}");
                database.AddBalance(args.Player.Name, (int)totalPrice);
            }
            #endregion
            #region List
            else if (args.Parameters[0] == "list")
            {
                if (!Utils.Util.CheckPermission(Permissions.List, args)) return;

                int pageSize = 5;
                int currentPage = 1;
                int totalPages = (int)Math.Ceiling(shopConfig.ShopItems.Count / (double)pageSize);

                if (args.Parameters.Count > 1 && int.TryParse(args.Parameters[1], out int parsedPage))
                {
                    if (totalPages == 0)
                    {
                        args.Player.SendErrorMessage("No items available");
                        return;
                    }
                    currentPage = Math.Clamp(parsedPage, 1, totalPages);
                }

                var itemsToDisplay = shopConfig.ShopItems
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
                        bool itemCanUseThePrefix = TShock.Utils.GetItemById(itemId).CanApplyPrefix(item.Value.prefix);
                        if (itemCanUseThePrefix)
                        {
                            prefix = item.Value.prefix;
                            prefixName = TShock.Utils.GetPrefixById(item.Value.prefix);
                        }
                    }

                    message += $"\n{index}. [i/p{prefix}:{item.Key}] ({item.Key}) {(!string.IsNullOrWhiteSpace(prefixName) ? "[" + prefixName + "] " : "")}- B: {Utils.Util.CurrencyFormat(item.Value.buyPrice)} | S: {Utils.Util.CurrencyFormat(item.Value.sellPrice)}";
                    index++;
                }


                args.Player.SendInfoMessage(message);
            }
            else
            {
                args.Player.SendErrorMessage(shopUsage);
            }
            #endregion
        }
    }
}
