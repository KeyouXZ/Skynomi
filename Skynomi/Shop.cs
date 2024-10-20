using System.Timers;
using TShockAPI;

namespace Skynomi {
    public class SkyShop {
        private static Config? config;
        private static SkyDatabase? database;
        private static System.Timers.Timer? broadcastTimer;
        public static void Initialize() {
            config = Config.Read();
            
            // broadcast
            if (config.AutoBroadcastShop) {
                broadcastTimer = new System.Timers.Timer(config.BroadcastIntervalInSeconds * 1000);
                broadcastTimer.Elapsed += OnBroadcastTimerElapsed;
                broadcastTimer.AutoReset = true;
                broadcastTimer.Start();
            }

            Commands.ChatCommands.Add(new Command("skynomi.buy", Buy, "buy"));
            Commands.ChatCommands.Add(new Command("skynomi.list", List, "list"));
        }

        public static void Reload() {
            if (config.AutoBroadcastShop) {
                broadcastTimer.Stop();
            }

            config = Config.Read();

            if (config.AutoBroadcastShop) {
                broadcastTimer = new System.Timers.Timer(config.BroadcastIntervalInSeconds * 1000);
                broadcastTimer.Elapsed += OnBroadcastTimerElapsed;
                broadcastTimer.AutoReset = true;
                broadcastTimer.Start();
            }

        }

        private static void Buy(CommandArgs args) {
            try {
            string usage = "Usage: /buy <item> [amount]";

            if (args.Parameters.Count < 1) {
                args.Player.SendErrorMessage(usage);
                return;
            } else if (!int.TryParse(args.Parameters[0], out int amount)) {
                args.Player.SendErrorMessage("Invalid item ID");
                return;
            } else if (args.Parameters.Count >= 2 && !int.TryParse(args.Parameters[1], out int amount1)) {
                args.Player.SendErrorMessage("Invalid amount");
                return;
            }

            int itemAmount = args.Parameters.Count > 1 ? int.Parse(args.Parameters[1]) : 1;

            // Check Item
            bool isThereAny = false;
            string itemKey = "1";
            decimal itemValue = 0;
            foreach (var item in config.ShopItems) {
                if (args.Parameters[0] == item.Key) {
                    itemKey = item.Key;
                    itemValue = item.Value;
                    isThereAny = true;
                    break;
                }
            }

            // check item
            if (!isThereAny) {
                args.Player.SendErrorMessage("Item not found");
                return;
            }

            // check balance
            decimal balance = SkyDatabase.GetBalance(args.Player.Name);
            int itemId = int.Parse(itemKey);

            if (TShock.Utils.GetItemById(int.Parse(itemKey)).maxStack == 1) {
                if (itemAmount > 1) {
                    args.Player.SendErrorMessage("This item can only be bought one at a time.");
                    return;
                }
                itemAmount = 1;
            }

            if (balance < itemValue) {
                args.Player.SendErrorMessage($"You do not have enough {config.Currency} to buy this item. ({balance - itemValue})");
                return;
            }

            args.Player.SendInfoMessage($"You have bought [i/s{itemAmount}:{args.Parameters[0]}]");
            args.Player.GiveItem(itemId, itemAmount);
            SkyDatabase.RemoveBalance(args.Player.Name, (int)itemValue);
            
            } catch (Exception ex) {
                TShock.Log.ConsoleError(ex.ToString());
            }
        }

        private static void List(CommandArgs args) {
            // shop list
            args.Player.SendInfoMessage(_List());
        }

        private static string _List() {
            // shop list
            string message = "Shop Items";
            int i = 0;
            foreach (var item in config.ShopItems) {
                i++;
                message += $"\n{i}. [i:{item.Key}] ({item.Key}) - {item.Value} {config.Currency}";
            }

            if (message == "Available Items") {
                message = "No items available";
            }

            return message;
        }

        // Broadcast
        private static void OnBroadcastTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Broadcast message to all players
            TSPlayer.All.SendInfoMessage(_List());
        }
    }
}