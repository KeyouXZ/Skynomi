using TShockAPI;

namespace Skynomi.Utils
{
    public class Util
    {
        private static Skynomi.Config config;
        public static void Initialize()
        {
            config = Skynomi.Config.Read();
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
        public static string CurrencyFormat(int amount)
        {
            return config.CurrencyFormat.Replace("{currency}", config.Currency).Replace("{amount}", amount.ToString());
        }
    }

}
