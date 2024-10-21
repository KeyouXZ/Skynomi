using System.Timers;
using TShockAPI;

namespace Skynomi {
    public class SkyShop {
        private static Config config;
        private static SkyDatabase database;
        private static System.Timers.Timer broadcastTimer;
        public static void Initialize() {
            config = Config.Read();
            
            // broadcast
            if (config.AutoBroadcastShop && _List() != "No items available") {
                broadcastTimer = new System.Timers.Timer(config.BroadcastIntervalInSeconds * 1000);
                broadcastTimer.Elapsed += OnBroadcastTimerElapsed;
                broadcastTimer.AutoReset = true;
                broadcastTimer.Start();
            }
        }

        public static void Reload() {
            if (config.AutoBroadcastShop) {
                broadcastTimer.Stop();
            }

            config = Config.Read();

            if (config.AutoBroadcastShop && _List() != "No items available") {
                broadcastTimer = new System.Timers.Timer(config.BroadcastIntervalInSeconds * 1000);
                broadcastTimer.Elapsed += OnBroadcastTimerElapsed;
                broadcastTimer.AutoReset = true;
                broadcastTimer.Start();
            }

        }
        public static string _List()
        {
            // shop list
            string message = "Shop Items";
            int i = 0;
            foreach (var item in config.ShopItems)
            {
                i++;
                message += $"\n{i}. [i:{item.Key}] ({item.Key}) - {item.Value} {config.Currency}";
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