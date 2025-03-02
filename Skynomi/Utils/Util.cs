using TShockAPI;
using Terraria;

namespace Skynomi.Utils
{
    public class Util
    {
        private static Skynomi.Config config;
        public static void Initialize()
        {
            config = Skynomi.Config.Read();
            On.OTAPI.Hooks.MessageBuffer.InvokeGetData += Skynomi.Utils.Util.OnGetData;
        }

        public static void Reload()
        {
            config = Skynomi.Config.Read();
        }
        public static bool CheckIfLogin(CommandArgs args)
        {
            if (!args.Player.IsLoggedIn)
            {
                args.Player.SendErrorMessage(Skynomi.Utils.Messages.NotLogged);
                return false;
            }
            return true;
        }

        public static bool CheckPermission(string perm, CommandArgs args)
        {
            if (!args.Player.HasPermission(perm))
            {
                args.Player.SendErrorMessage(Skynomi.Utils.Messages.PermissionError, TShockAPI.Commands.Specifier);
                return false;
            }

            return true;
        }

        #region Currency
        public static string CurrencyFormat(int amount)
        {   
            string formattedAmount;
            if (config.AbbsreviasiNumeric) {
                formattedAmount = FormatNumber(amount);
            } else {
                formattedAmount = amount.ToString();
            }
            return config.CurrencyFormat.Replace("{currency}", config.Currency).Replace("{amount}", formattedAmount.ToString());
        }

        private static string FormatNumber(int num)
        {
            if (num >= 1_000_000_000)
                return (num / 1_000_000_000D).ToString("0.#") + "B";
            if (num >= 1_000_000)
                return (num / 1_000_000D).ToString("0.#") + "M";
            if (num >= 1_000)
                return (num / 1_000D).ToString("0.#") + "K";

            return num.ToString();
        }
        #endregion

        #region Platform
        public enum PlatformType : byte
        {
            MOBILE = 0,
            Stadia = 1,
            XBOX = 2,
            PSN = 3,
            Editor = 4,
            Switch = 5,
            PC = 233
        }

        public static PlatformType[] Platforms { get; set; } = new PlatformType[256];

        public static string GetPlatform(TSPlayer player)
        {
            return Platforms[player.Index].ToString();
        }

        private static bool OnGetData(On.OTAPI.Hooks.MessageBuffer.orig_InvokeGetData orig, MessageBuffer instance, ref byte packetId, ref int readOffset, ref int start, ref int length, ref int messageType, int maxPackets)
        {
            try
            {
                if (messageType == 1)
                {
                    Platforms[instance.whoAmI] = PlatformType.PC;
                }

                if (messageType == 150)
                {
                    instance.ResetReader();
                    instance.reader.BaseStream.Position = start + 1;
                    var PlayerSlot = instance.reader.ReadByte();
                    var Platform = instance.reader.ReadByte();
                    Platforms[instance.whoAmI] = (PlatformType)Platform;
                }
            }
            catch { }

            return orig(instance, ref packetId, ref readOffset, ref start, ref length, ref messageType, maxPackets);
        }
        #endregion
    }
}
