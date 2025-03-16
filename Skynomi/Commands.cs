using System.Text;
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

            TShockAPI.Commands.ChatCommands.Add(new Command(Skynomi.Utils.Permissions.Pay, Pay, "pay")
            {
                AllowServer = false,
                HelpText = "Allows a player to send currency to another player."
            });
            TShockAPI.Commands.ChatCommands.Add(new Command(Skynomi.Utils.Permissions.Balance, Balance, "balance", "bal")
            {
                AllowServer = true,
                HelpText = "Displays the player's current currency balance."
            });
            TShockAPI.Commands.ChatCommands.Add(new Command(Skynomi.Utils.Permissions.Admin, Admin, "admin")
            {
                AllowServer = true,
                HelpText = "Admin commands:\nsetbal <player> <amount> - Sets the balance of a player."
            });
            TShockAPI.Commands.ChatCommands.Add(new Command(Skynomi.Utils.Permissions.Skynomi, SkynomiCmd, "skynomi", "sk")
            {
                AllowServer = true,
                HelpText = "Use /skynomi help to display all commands."
            });
        }

        public static void Reload()
        {
            config = Config.Read();
        }

        // Commands
        public static void Pay(CommandArgs args)
        {
            #region Pay
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

            long balancePlayer = database.GetBalance(args.Player.Name);
            long balanceTarget = database.GetBalance(targetPlayer.Name);


            // Check if the player has enough balance to pay
            if (!long.TryParse(args.Parameters[1], out long amount))
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
            #endregion
        }

        public static void Balance(CommandArgs args)
        {
            #region Balance
            try
            {
                if (args.Player == null)
                    return;


                string targetUsername = args.Parameters.Count > 0 ? args.Parameters[0] : args.Player.Name;

                var targetPlayer = TShock.Players
                .Where(p => p != null && p.Name.Equals(targetUsername, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

                if (args.Player == TShockAPI.TSPlayer.Server && args.Parameters.Count == 0)
                {
                    args.Player.SendErrorMessage("You cannot see your balance from the console.");
                    return;
                }

                if (targetPlayer == null)
                {
                    args.Player.SendErrorMessage($"Player '{targetUsername}' not found.");
                    return;
                }

                long balance = database.GetBalance(targetPlayer.Name);

                if (args.Parameters.Count == 0)
                {
                    targetUsername = "Your";
                }
                else
                {
                    targetUsername = $"{targetPlayer.Name}'s";
                }

                args.Player.SendInfoMessage($"{targetUsername} balance: {Skynomi.Utils.Util.CurrencyFormat(balance)}");
                #endregion
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
                #region Setbal
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

                long balanceTarget = database.GetBalance(targetPlayer.Name);

                if (!int.TryParse(args.Parameters[2], out int amount))
                {
                    args.Player.SendErrorMessage("Invalid amount.");
                    return;
                }

                database.AddBalance(targetPlayer.Name, (int)amount);
                args.Player.SendSuccessMessage($"Successfully gave {Skynomi.Utils.Util.CurrencyFormat(amount)} to {targetUsername}");
                #endregion
            }
            else
            {
                args.Player.SendErrorMessage("Usage: /admin <command>");
                args.Player.SendSuccessMessage("Command list:");
                args.Player.SendErrorMessage(usage);
                return;
            }
        }

        public static void SkynomiCmd(CommandArgs args)
        {
            string usage = "Usage: /skynomi <command>";

            var sb = new StringBuilder();
            sb.AppendLine("Command list:");
            sb.AppendLine("- cache <command/help> - Cache commands");

            if (args.Parameters.Count == 0)
            {
                args.Player.SendInfoMessage(usage);
                return;
            }
            else if (args.Parameters[0] == "help")
            {
                args.Player.SendInfoMessage(sb.ToString());
                return;
            }
            else if (args.Parameters[0] == "cache")
            {
                #region Cache
                string cacheUsage = "Usage: /skynomi cache <command/help>";
                if (args.Parameters.Count < 2)
                {
                    args.Player.SendInfoMessage(cacheUsage);
                    return;
                }
                else if (args.Parameters[1] == "help")
                {
                    var cacheSb = new StringBuilder();
                    cacheSb.AppendLine("Cache commands:");
                    cacheSb.AppendLine("- list - List all cache keys");
                    cacheSb.AppendLine("- reload <cache> [save] - Reload all cache data from the database");
                    cacheSb.AppendLine("- save <cache> - Save all cache data to the database");
                    cacheSb.AppendLine("- reloadall [save] - Reload all cache data from the database");
                    cacheSb.AppendLine("- saveall - Save all cache data to the database");
                    args.Player.SendInfoMessage(cacheSb.ToString());
                    return;
                }
                else if (args.Parameters[1] == "list")
                {
                    var caches = Skynomi.Database.CacheManager.GetAllCacheKeys();
                    var cacheSb = new StringBuilder();
                    cacheSb.AppendLine("Cache list:");
                    foreach (var cache in caches)
                    {
                        cacheSb.AppendLine($"- {cache}");
                    }
                    args.Player.SendInfoMessage(cacheSb.ToString());
                    return;
                }
                else if (args.Parameters[1] == "reload")
                {
                    #region ReloadCache
                    if (args.Parameters.Count < 3)
                    {
                        args.Player.SendErrorMessage("Usage: /skynomi cache reload <cache> [save]");
                        return;
                    }

                    if (!Skynomi.Database.CacheManager.GetAllCacheKeys().Contains(args.Parameters[2]))
                    {
                        args.Player.SendErrorMessage("Cache not found.");
                        return;
                    }

                    var cache = Skynomi.Database.CacheManager.Cache.GetCache<object>(args.Parameters[2]);

                    bool save;
                    if (args.Parameters.Count > 3)
                    {
                        if (bool.TryParse(args.Parameters[3], out save)) { }
                        else
                        {
                            args.Player.SendErrorMessage("Invalid bool for [save].");
                            return;
                        }
                    }
                    else
                    {
                        save = false;
                    }
                    bool status = cache.Reload(save);

                    if (status)
                    {
                        args.Player.SendSuccessMessage($"Reloaded {args.Parameters[2]} cache" + (!save ? " without saving" : ""));
                    }
                    return;
                    #endregion
                }
                else if (args.Parameters[1] == "save")
                {
                    #region SaveCache
                    if (args.Parameters.Count < 3)
                    {
                        args.Player.SendErrorMessage("Usage: /skynomi cache save <cache>");
                        return;
                    }

                    if (!Skynomi.Database.CacheManager.GetAllCacheKeys().Contains(args.Parameters[2]))
                    {
                        args.Player.SendErrorMessage("Cache not found.");
                        return;
                    }

                    var cache = Skynomi.Database.CacheManager.Cache.GetCache<object>(args.Parameters[2]);
                    bool status = cache.Save();

                    if (status)
                    {
                        args.Player.SendSuccessMessage($"Saved {args.Parameters[2]} cache");
                    }
                    return;
                    #endregion
                }
                else if (args.Parameters[1] == "reloadall")
                {
                    #region ReloadAllCache
                    var allCacheKeys = Skynomi.Database.CacheManager.GetAllCacheKeys();
                    bool save = args.Parameters.Count > 2 && bool.TryParse(args.Parameters[2], out var saveParam) && saveParam;

                    foreach (var cacheKey in allCacheKeys)
                    {
                        var cache = Skynomi.Database.CacheManager.Cache.GetCache<object>(cacheKey);
                        bool status = cache.Reload(save);

                        if (status)
                        {
                            args.Player.SendSuccessMessage($"Reloaded {cacheKey} cache" + (!save ? " without saving" : ""));
                        }
                        else
                        {
                            args.Player.SendErrorMessage($"Failed to reload {cacheKey} cache");
                        }
                    }
                    return;
                    #endregion
                }
                else if (args.Parameters[1] == "saveall")
                {
                    #region SaveAllCache
                    var allCacheKeys = Skynomi.Database.CacheManager.GetAllCacheKeys();

                    foreach (var cacheKey in allCacheKeys)
                    {
                        var cache = Skynomi.Database.CacheManager.Cache.GetCache<object>(cacheKey);
                        bool status = cache.Save();

                        if (status)
                        {
                            args.Player.SendSuccessMessage($"Saved {cacheKey} cache");
                        }
                        else
                        {
                            args.Player.SendErrorMessage($"Failed to save {cacheKey} cache");
                        }
                    }
                    return;
                    #endregion
                }
                else
                {
                    args.Player.SendErrorMessage(cacheUsage);
                    return;
                }
                #endregion
            }
            else
            {
                args.Player.SendErrorMessage("Usage: /skynomi <command>");
                return;
            }
        }
    }
}
