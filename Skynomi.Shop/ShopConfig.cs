namespace Skynomi.ShopSystem
{
    public class Permissions
    {
        public static readonly string Shop = "skynomi.shop";
        public static readonly string List = "skynomi.shop.list";
        public static readonly string Buy = "skynomi.shop.buy";
    }

    public class Messages
    {
        public static readonly string AutoShopDisabled = $"{Skynomi.Utils.Messages.Name} Auto shop broadcast is disabled! Add item in config to enable!";
        public static readonly string EmptyNEnableProtectedRegion = $"{Skynomi.Utils.Messages.Name} Protected region is enabled but no region is set. Please set a region in the config.";

    }
}