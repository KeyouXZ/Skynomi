using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Org.BouncyCastle.Math.EC.ECCurve;
using TShockAPI;

namespace Skynomi
{
    public class SkyCommands {
        private static Config config;
        public static void Initialize()
        {
            config = Config.Read();
        }

        public static void Reload()
        {
            config = Config.Read();
        }

        public static bool CheckIfLogin(CommandArgs args)
        {
            if (!args.Player.IsLoggedIn)
            {
                args.Player.SendErrorMessage("You must be logged in to use this commands");
                return false;
            }
            return true;
        }
        // Commands
        public static void Balance(CommandArgs args)
        {
            try
            {
                // Login is required
                if (!CheckIfLogin(args))
                    return;

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

                decimal balance = SkyDatabase.GetBalance(targetPlayer.Name);

                if (args.Parameters.Count == 0)
                {
                    targetUsername = "Your";
                }
                else
                {
                    targetUsername = $"{targetPlayer.Name}'s";
                }

                args.Player.SendInfoMessage($"{targetUsername} balance: {balance} {config.Currency}");
            }
            catch (Exception ex)
            {
                args.Player.SendErrorMessage(ex.ToString());
                return;
            }
        }

        public static void Pay(CommandArgs args)
        {
            // Login is required
            if(!CheckIfLogin(args))
                return;

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

            decimal balancePlayer = SkyDatabase.GetBalance(args.Player.Name);
            decimal balanceTarget = SkyDatabase.GetBalance(targetPlayer.Name);


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

            SkyDatabase.RemoveBalance(args.Player.Name, amount);
            SkyDatabase.AddBalance(targetPlayer.Name, amount);

            args.Player.SendInfoMessage($"You have paid {amount} {config.Currency} to {targetPlayer.Name}.");
            targetPlayer.SendInfoMessage($"You have received {amount} {config.Currency} from {args.Player.Name}.");
        }

        public static void Shop(CommandArgs args)
        {
            if (!CheckIfLogin(args))
                return;

            string shopUsage = "Usage: /shop <buy/list>";
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage(shopUsage);
                return;
            }

            if (args.Parameters[0] == "buy")
            {
                if (!args.Player.HasPermission(Permissions.Buy))
                {
                    args.Player.SendErrorMessage("[Skynomi] You don't have permission to use this command", Commands.Specifier);
                    return;
                }

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
                    foreach (var item in config.ShopItems)
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
                    decimal balance = SkyDatabase.GetBalance(args.Player.Name);
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
                        args.Player.SendErrorMessage($"You do not have enough {config.Currency} to buy this item. (Need {itemValue - balance} more)");
                        return;
                    }

                    args.Player.SendInfoMessage($"You have bought [i/s{itemAmount}:{args.Parameters[1]}]");
                    args.Player.GiveItem(itemId, itemAmount);
                    SkyDatabase.RemoveBalance(args.Player.Name, (int)itemValue);

                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleError(ex.ToString());
                }
            } 
            else if (args.Parameters[0] == "list")
            {
                if (!args.Player.HasPermission(Permissions.List))
                {
                    args.Player.SendErrorMessage("[Skynomi] You don't have permission to use this command", Commands.Specifier);
                    return;
                }

                args.Player.SendInfoMessage(SkyShop._List());
            }
        }
    }
}
