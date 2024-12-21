using TShockAPI;

namespace Skynomi
{
    public class Commands
    {
        private static Config config;
        private static Skynomi.Database.Database database = new Database.Database();
        public static void Initialize()
        {
            config = Config.Read();

            TShockAPI.Commands.ChatCommands.Add(new Command(Skynomi.Utils.Permissions.List, Pay, "pay")
            {
                AllowServer = false,
                HelpText = "Allows a player to send currency to another player."
            });
            TShockAPI.Commands.ChatCommands.Add(new Command(Skynomi.Utils.Permissions.Balance, Balance, "balance", "bal")
            {
                AllowServer = false,
                HelpText = "Displays the player's current currency balance."
            });
            TShockAPI.Commands.ChatCommands.Add(new Command(Skynomi.Utils.Permissions.Admin, Admin, "admin")
            {
                AllowServer = true,
                HelpText = "Admin commands:\nsetbal <player> <amount> - Sets the balance of a player."
            });
        }

        public static void Reload()
        {
            config = Config.Read();
        }

        // Commands
        public static void Pay(CommandArgs args)
        {
            if (args.Player == null)
                return;

            string usage = "Usage: /pay <player> <amount>";

            // send usage message when only using /pay
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage(usage);
                return;
            }

            string targetUsername = args.Parameters.Count > 0 ? args.Parameters[0] : args.Player.Name;

            var targetPlayer = TShock.Players
            .Where(p => p != null && p.Name.Equals(targetUsername, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

            if (targetPlayer == null)
            {
                args.Player.SendErrorMessage($"Player '{targetUsername}' not found.");
                return;
            }
            else if (targetPlayer.Name == args.Player.Name)
            {
                args.Player.SendErrorMessage("You cannot pay yourself.");
                return;
            }

            decimal balancePlayer = database.GetBalance(args.Player.Name);
            decimal balanceTarget = database.GetBalance(targetPlayer.Name);


            // Check if the player has enough balance to pay
            if (!int.TryParse(args.Parameters[1], out int amount))
            {
                args.Player.SendErrorMessage("Invalid amount.");
                return;
            }

            if (amount <= 0)
            {
                args.Player.SendErrorMessage("Amount must be greater than 0.");
                return;
            }

            if (balancePlayer < amount)
            {
                args.Player.SendErrorMessage($"You do not have enough {config.Currency} to pay.");
                return;
            }

            database.RemoveBalance(args.Player.Name, amount);
            database.AddBalance(targetPlayer.Name, amount);

            args.Player.SendInfoMessage($"You have paid {Skynomi.Utils.Util.CurrencyFormat(amount)} to {targetPlayer.Name}.");
            targetPlayer.SendInfoMessage($"You have received {Skynomi.Utils.Util.CurrencyFormat(amount)} from {args.Player.Name}.");
        }

        public static void Balance(CommandArgs args)
        {
            try
            {
                if (args.Player == null)
                    return;

                string targetUsername = args.Parameters.Count > 0 ? args.Parameters[0] : args.Player.Name;

                var targetPlayer = TShock.Players
                .Where(p => p != null && p.Name.Equals(targetUsername, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

                if (targetPlayer == null)
                {
                    args.Player.SendErrorMessage($"Player '{targetUsername}' not found.");
                    return;
                }

                decimal balance = database.GetBalance(targetPlayer.Name);

                if (args.Parameters.Count == 0)
                {
                    targetUsername = "Your";
                }
                else
                {
                    targetUsername = $"{targetPlayer.Name}'s";
                }

                args.Player.SendInfoMessage($"{targetUsername} balance: {Skynomi.Utils.Util.CurrencyFormat((int)balance)}");
            }
            catch (Exception ex)
            {
                args.Player.SendErrorMessage(ex.ToString());
                return;
            }
        }

        // Admin commands
        public static void Admin(CommandArgs args)
        {
            string usage = $"setbal: Set player's {config.Currency} to a specific amount. Use - to reduce user currency";

            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("Usage: /admin <command>");
                args.Player.SendInfoMessage("Command list:");
                args.Player.SendInfoMessage(usage);
                return;
            }
            else if (args.Parameters[0] == "setbal")
            {
                if (!Skynomi.Utils.Util.CheckPermission(Skynomi.Utils.Permissions.AdminBalance, args)) return;
                string SetbalUsage = "Usage: /admin setbal <player> <amount>";

                if (args.Parameters.Count < 2)
                {
                    args.Player.SendErrorMessage(SetbalUsage);
                    return;
                }

                string targetUsername = args.Parameters.Count > 1 ? args.Parameters[1] : args.Player.Name;

                var targetPlayer = TShock.Players
                .Where(p => p != null && p.Name.Equals(targetUsername, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

                if (targetPlayer == null)
                {
                    args.Player.SendErrorMessage($"Player '{targetUsername}' not found.");
                    return;
                }

                decimal balanceTarget = database.GetBalance(targetPlayer.Name);

                if (!int.TryParse(args.Parameters[2], out int amount))
                {
                    args.Player.SendErrorMessage("Invalid amount.");
                    return;
                }

                database.AddBalance(targetPlayer.Name, (int)amount);
                args.Player.SendSuccessMessage($"Successfully gave {Skynomi.Utils.Util.CurrencyFormat(amount)} to {targetUsername}");
            }
            else
            {
                args.Player.SendErrorMessage("Usage: /admin <command>");
                args.Player.SendSuccessMessage("Command list:");
                args.Player.SendErrorMessage(usage);
                return;
            }
        }
    }
}
