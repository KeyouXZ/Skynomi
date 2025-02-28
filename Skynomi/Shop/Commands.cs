using TShockAPI;
using TShockAPI.DB;

namespace Skynomi.ShopSystem
{
    public static class Commands
    {
        private static Skynomi.Config config;
        private static Skynomi.ShopSystem.Config shopConfig;
        private static Skynomi.Database.Database database = new Database.Database();

        public static void Initialize()
        {
            config = Skynomi.Config.Read();
            shopConfig = Skynomi.ShopSystem.Config.Read();

            TShockAPI.Commands.ChatCommands.Add(new Command(Skynomi.Utils.Permissions.Shop, Shop, "shop")
            {
                AllowServer = false,
                HelpText = "Shop commands:\nbuy <item> [amount] - Buy an item\nlist - List all items in the shop"
            });
        }

        public static void Reload()
        {
            config = Skynomi.Config.Read();
            shopConfig = Skynomi.ShopSystem.Config.Read();
        }

        public static void Shop(CommandArgs args)
        {
            string shopUsage = "Usage: /shop <buy/list>";
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

            if (args.Parameters[0] == "buy")
            {
                if (!Skynomi.Utils.Util.CheckPermission(Skynomi.Utils.Permissions.Buy, args)) return;

                try
                {
                    string usage = "Usage: /shop buy <item> [amount]";

                    if (args.Parameters.Count < 2)
                    {
                        args.Player.SendErrorMessage(usage);
                        return;
                    }
                    else if (!int.TryParse(args.Parameters[1], out int amount))
                    {
                        args.Player.SendErrorMessage("Invalid item ID");
                        return;
                    }
                    else if (args.Parameters.Count >= 3 && !int.TryParse(args.Parameters[2], out int amount1))
                    {
                        args.Player.SendErrorMessage("Invalid amount");
                        return;
                    }

                    int itemAmount = args.Parameters.Count > 2 ? int.Parse(args.Parameters[2]) : 1;

                    // Check Item
                    bool isThereAny = false;
                    string itemKey = "1";
                    decimal itemValue = 0;
                    foreach (var item in shopConfig.ShopItems)
                    {
                        if (args.Parameters[1] == item.Key)
                        {
                            itemKey = item.Key;
                            itemValue = item.Value;
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
                    decimal balance = database.GetBalance(args.Player.Name);
                    int itemId = int.Parse(itemKey);

                    if (TShock.Utils.GetItemById(int.Parse(itemKey)).maxStack == 1)
                    {
                        if (itemAmount > 1)
                        {
                            args.Player.SendErrorMessage("This item can only be bought one at a time.");
                            return;
                        }
                        itemAmount = 1;
                    }

                    if (balance < itemValue)
                    {
                        args.Player.SendErrorMessage($"You do not have enough {config.Currency} to buy this item. (Need {Skynomi.Utils.Util.CurrencyFormat((int)(itemValue - balance))} more)");
                        return;
                    }

                    args.Player.SendInfoMessage($"You have bought [i/s{itemAmount}:{args.Parameters[1]}]");
                    args.Player.GiveItem(itemId, itemAmount);
                    database.RemoveBalance(args.Player.Name, (int)itemValue);

                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleError(ex.ToString());
                }
            }
            else if (args.Parameters[0] == "list")
            {
                if (!Skynomi.Utils.Util.CheckPermission(Skynomi.Utils.Permissions.List, args)) return;

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
                    message += $"\n{index}. [i:{item.Key}] ({item.Key}) - {Skynomi.Utils.Util.CurrencyFormat(item.Value)}";
                    index++;
                }


                args.Player.SendInfoMessage(message);
            }
            else
            {
                args.Player.SendErrorMessage(shopUsage);
                return;
            }
        }
    }
}
