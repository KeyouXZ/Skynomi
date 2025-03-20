namespace Skynomi.ShopSystem
{
    public abstract class Permissions
    {
        public static readonly string Shop = "skynomi.shop";
        public static readonly string List = "skynomi.shop.list";
        public static readonly string Buy = "skynomi.shop.buy";
        public static readonly string Sell = "skynomi.shop.sell";
    }

    public abstract class Messages
    {
        public static readonly string AutoShopDisabled = "Auto shop broadcast is disabled! Add item in config to enable!";
        public static readonly string EmptyNEnableProtectedRegion = "Protected region is enabled but no region is set. Please set a region in the config.";

    }
}