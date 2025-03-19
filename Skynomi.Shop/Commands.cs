using Terraria;
using TShockAPI;

namespace Skynomi.ShopSystem
{
    public static class Commands
    {
        private static Skynomi.Config config;
        private static ShopSystem.Config shopConfig;
        private static Skynomi.Database.Database database = new Database.Database();

        public static void Initialize()
        {
            config = Skynomi.Config.Read();
            shopConfig = Skynomi.ShopSystem.Config.Read();

            TShockAPI.Commands.ChatCommands.Add(new Command(ShopSystem.Permissions.Shop, Shop, "shop")
            {
                AllowServer = false,
                HelpText = "Shop commands:\nbuy <item> [amount] - Buy an item\nsell <item> [amount] - Sell an item\nlist [page] - List all items in the shop"
            });
        }

        public static void Reload()
        {
            config = Skynomi.Config.Read();
            shopConfig = Skynomi.ShopSystem.Config.Read();
        }

        public static void Shop(CommandArgs args)
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
                if (!Skynomi.Utils.Util.CheckPermission(ShopSystem.Permissions.Buy, args)) return;

                try
                {
                    string usage = "Usage: /shop buy <item> [amount]";

                    int itemAmount = 1;
                    if (args.Parameters.Count < 2)
                    {
                        args.Player.SendErrorMessage(usage);
                        return;
                    }
                    else if (!int.TryParse(args.Parameters[1], out int itemID))
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
                    foreach (var item in shopConfig.ShopItems)
                    {
                        if (args.Parameters[1] == item.Key)
                        {
                            itemKey = item.Key;
                            itemValue = item.Value.buyPrice;
                            isThereAny = true;
                            break;
                        }
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
                        args.Player.SendErrorMessage($"You do not have enough {config.Currency} to buy this item. (Need {Skynomi.Utils.Util.CurrencyFormat((int)(totalPrice - balance))} more)");
                        return;
                    }

                    args.Player.SendInfoMessage($"You have bought [i/s{itemAmount}:{args.Parameters[1]}] for {Skynomi.Utils.Util.CurrencyFormat((int)(totalPrice))}");
                    args.Player.GiveItem(itemId, itemAmount);
                    database.RemoveBalance(args.Player.Name, totalPrice);

                }
                catch (Exception ex)
                {
                    Skynomi.Utils.Log.Error(ex.ToString());
                }
            }
            #endregion
            #region Sell
            else if (args.Parameters[0] == "sell")
            {
                if (!Skynomi.Utils.Util.CheckPermission(ShopSystem.Permissions.Sell, args)) return;

                string usage = "Usage: /shop sell <item> [amount]";

                if (args.Parameters.Count < 2)
                {
                    args.Player.SendErrorMessage(usage);
                    return;
                }

                int itemID;
                if (!int.TryParse(args.Parameters[1], out itemID))
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
                    NetMessage.SendData(7, args.Player.Index, -1, null, 0, 0.0f, 0.0f, 0.0f, 0, 0, 0);
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
                            NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, null, args.Player.Index, i, 0, 0, 0, 0, 0);
                            break;
                        }
                        else
                        {
                            remainingToRemove -= args.Player.TPlayer.inventory[i].stack;
                            args.Player.TPlayer.inventory[i].netDefaults(0);
                            NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, null, args.Player.Index, i, 0, 0, 0, 0, 0);
                        }
                    }
                }

                long totalPrice = itemValue * amount;
                args.Player.SendInfoMessage($"You have sell [i/s{amount}:{itemID}] for {Skynomi.Utils.Util.CurrencyFormat((int)totalPrice)}");
                database.AddBalance(args.Player.Name, (int)totalPrice);
            }
            #endregion
            #region List
            else if (args.Parameters[0] == "list")
            {
                if (!Skynomi.Utils.Util.CheckPermission(ShopSystem.Permissions.List, args)) return;

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
                    message += $"\n{index}. [i:{item.Key}] ({item.Key}) - B: {Skynomi.Utils.Util.CurrencyFormat(item.Value.buyPrice)} | S: {Skynomi.Utils.Util.CurrencyFormat(item.Value.sellPrice)}";
                    index++;
                }


                args.Player.SendInfoMessage(message);
            }
            else
            {
                args.Player.SendErrorMessage(shopUsage);
                return;
            }
            #endregion
        }
    }
}
