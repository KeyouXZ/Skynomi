using System.Timers;
using Skynomi.Utils;
using TShockAPI;
using TShockAPI.Hooks;

namespace Skynomi.ShopSystem
{
    public class Shop : Loader.ISkynomiExtension, Loader.ISkynomiExtensionReloadable, Loader.ISkynomiExtensionPostInit
    {
        public string Name => "Shop System";
        public string Description => "Shop system extension for Skynomi";
        public Version Version => new Version(1, 1, 1);
        public string Author => "Keyou";

        private static Skynomi.Config config;
        private static ShopSystem.Config shopConfig;
        private static System.Timers.Timer broadcastTimer;
        public void Initialize()
        {
            config = Skynomi.Config.Read();
            shopConfig = ShopSystem.Config.Read();

            ShopSystem.Commands.Initialize();
        }

        public void Reload(ReloadEventArgs args)
        {
            if (shopConfig.AutoBroadcastShop)
            {
                broadcastTimer.Stop();
            }

            config = Skynomi.Config.Read();
            shopConfig = ShopSystem.Config.Read();

            ShopSystem.Commands.Reload();

            if (shopConfig.ProtectedByRegion && string.IsNullOrEmpty(shopConfig.ShopRegion))
            {
                TShock.Log.ConsoleWarn(ShopSystem.Messages.EmptyNEnableProtectedRegion);
            }

            if (shopConfig.AutoBroadcastShop && _List() != "No items available")
            {
                TShock.Log.Warn(ShopSystem.Messages.AutoShopDisabled);
                broadcastTimer = new System.Timers.Timer(shopConfig.BroadcastIntervalInSeconds * 1000);
                broadcastTimer.Elapsed += OnBroadcastTimerElapsed;
                broadcastTimer.AutoReset = true;
                broadcastTimer.Start();
            }
        }

        public void PostInitialize(EventArgs args)
        {
            // broadcast
            if (shopConfig.AutoBroadcastShop && _List() != "No items available")
            {
                TShock.Log.Warn(ShopSystem.Messages.AutoShopDisabled);
                broadcastTimer = new System.Timers.Timer(shopConfig.BroadcastIntervalInSeconds * 1000);
                broadcastTimer.Elapsed += OnBroadcastTimerElapsed;
                broadcastTimer.AutoReset = true;
                broadcastTimer.Start();
            }

            if (shopConfig.ProtectedByRegion && string.IsNullOrEmpty(shopConfig.ShopRegion))
            {
                TShock.Log.ConsoleWarn(ShopSystem.Messages.EmptyNEnableProtectedRegion);
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
                message += $"\n{i}. [i:{item.Key}] ({item.Key}) - B: {Skynomi.Utils.Util.CurrencyFormat(item.Value.buyPrice)} | S: {Skynomi.Utils.Util.CurrencyFormat(item.Value.sellPrice)}";
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