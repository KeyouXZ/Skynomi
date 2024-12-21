using System.Timers;
using TShockAPI;

namespace Skynomi.ShopSystem {
    public class Shop {
        private static Skynomi.Config config;
        private static Skynomi.ShopSystem.Config shopConfig;
        private static System.Timers.Timer broadcastTimer;
        public static void Initialize() {
            config = Skynomi.Config.Read();
            shopConfig = Skynomi.ShopSystem.Config.Read();

            Skynomi.ShopSystem.Commands.Initialize();
            
            // broadcast
            if (shopConfig.AutoBroadcastShop && _List() != "No items available") {
                TShock.Log.Warn(Skynomi.Utils.Messages.AutoShopDisabled);
                broadcastTimer = new System.Timers.Timer(shopConfig.BroadcastIntervalInSeconds * 1000);
                broadcastTimer.Elapsed += OnBroadcastTimerElapsed;
                broadcastTimer.AutoReset = true;
                broadcastTimer.Start();
            }
        }

        public static void Reload() {
            if (shopConfig.AutoBroadcastShop) {
                broadcastTimer.Stop();
            }

            config = Skynomi.Config.Read();
            shopConfig = Skynomi.ShopSystem.Config.Read();

            Skynomi.ShopSystem.Commands.Reload();

            if (shopConfig.ProtectedByRegion && string.IsNullOrEmpty(shopConfig.ShopRegion))
            {
                TShock.Log.ConsoleWarn(Skynomi.Utils.Messages.EmptyNEnableProtectedRegion);
            }

            if (shopConfig.AutoBroadcastShop && _List() != "No items available") {
                TShock.Log.Warn(Skynomi.Utils.Messages.AutoShopDisabled);
                broadcastTimer = new System.Timers.Timer(shopConfig.BroadcastIntervalInSeconds * 1000);
                broadcastTimer.Elapsed += OnBroadcastTimerElapsed;
                broadcastTimer.AutoReset = true;
                broadcastTimer.Start();
            }
        }

        public static void PostInitialize()
        {
            if (shopConfig.ProtectedByRegion && string.IsNullOrEmpty(shopConfig.ShopRegion))
            {
                TShock.Log.ConsoleWarn(Skynomi.Utils.Messages.EmptyNEnableProtectedRegion);
            }
        }

        public static string _List()
        {
            // shop list
            string message = "Shop Items";
            int i = 0;
            foreach (var item in shopConfig.ShopItems)
            {
                i++;
                message += $"\n{i}. [i:{item.Key}] ({item.Key}) - {Skynomi.Utils.Util.CurrencyFormat(item.Value)}";
            }

            if (message == "Shop Items")
            {
                message = "No items available";
            }

            return message;
        }

        // Broadcast
        private static void OnBroadcastTimerElapsed(object sender, ElapsedEventArgs e)
        {
            TSPlayer.All.SendInfoMessage(_List());
        }
    }
}