using System.Text;
using TShockAPI;

namespace Skynomi
{
    public abstract class Commands
    {
        private static Config _config;
        private static readonly Skynomi.Database.Database Database = new();
        public static void Initialize()
        {
            _config = Config.Read();

            TShockAPI.Commands.ChatCommands.Add(new Command(Utils.Permissions.Pay, Pay, "pay")
            {
                AllowServer = false,
                HelpText = "Allows a player to send currency to another player."
            });
            TShockAPI.Commands.ChatCommands.Add(new Command(Utils.Permissions.Balance, Balance, "balance", "bal")
            {
                AllowServer = true,
                HelpText = "Displays the player's current currency balance."
            });
            TShockAPI.Commands.ChatCommands.Add(new Command(Utils.Permissions.Admin, Admin, "admin")
            {
                AllowServer = true,
                HelpText = "Admin commands:\nsetbal <player> <amount> - Sets the balance of a player."
            });
            TShockAPI.Commands.ChatCommands.Add(new Command(Utils.Permissions.Skynomi, SkynomiCmd, "skynomi", "sk")
            {
                AllowServer = true,
                HelpText = "Use /skynomi help to display all commands."
            });
        }

        public static void Reload()
        {
            _config = Config.Read();
        }

        // Commands
        private static void Pay(CommandArgs args)
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
            .Where(p =>
            {
                if (p != null && p.Name.Equals(targetUsername, StringComparison.OrdinalIgnoreCase)) return true;
                return false;
            })
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

            long balancePlayer = Database.GetBalance(args.Player.Name);

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
                args.Player.SendErrorMessage($"You do not have enough {_config.Currency} to pay.");
                return;
            }

            Database.RemoveBalance(args.Player.Name, amount);
            Database.AddBalance(targetPlayer.Name, amount);

            args.Player.SendInfoMessage($"You have paid {Utils.Util.CurrencyFormat(amount)} to {targetPlayer.Name}.");
            targetPlayer.SendInfoMessage($"You have received {Utils.Util.CurrencyFormat(amount)} from {args.Player.Name}.");
            #endregion
        }

        private static void Balance(CommandArgs args)
        {
            #region Balance
            try
            {
                if (args.Player == null)
                    return;


                string targetUsername = args.Parameters.Count > 0 ? args.Parameters[0] : args.Player.Name;

                var targetPlayer = TShock.Players
                .Where(p =>
                {
                    if (p != null && p.Name.Equals(targetUsername, StringComparison.OrdinalIgnoreCase)) return true;
                    return false;
                })
                .FirstOrDefault();

                if (args.Player == TSPlayer.Server && args.Parameters.Count == 0)
                {
                    args.Player.SendErrorMessage("You cannot see your balance from the console.");
                    return;
                }

                if (targetPlayer == null)
                {
                    args.Player.SendErrorMessage($"Player '{targetUsername}' not found.");
                    return;
                }

                long balance = Database.GetBalance(targetPlayer.Name);

                targetUsername = args.Parameters.Count == 0 ? "Your" : $"{targetPlayer.Name}'s";

                args.Player.SendInfoMessage($"{targetUsername} balance: {Utils.Util.CurrencyFormat(balance)}");
                #endregion
            }
            catch (Exception ex)
            {
                args.Player.SendErrorMessage(ex.ToString());
            }
        }

        // Admin commands
        private static void Admin(CommandArgs args)
        {
            string usage = $"setbal: Set player's {_config.Currency} to a specific amount. Use - to reduce user currency";

            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("Usage: /admin <command>");
                args.Player.SendInfoMessage("Command list:");
                args.Player.SendInfoMessage(usage);
            }
            else if (args.Parameters[0] == "setbal")
            {
                #region Setbal
                if (!Utils.Util.CheckPermission(Utils.Permissions.AdminBalance, args)) return;
                string setbalUsage = "Usage: /admin setbal <player> <amount>";

                if (args.Parameters.Count < 2)
                {
                    args.Player.SendErrorMessage(setbalUsage);
                    return;
                }

                string targetUsername = args.Parameters.Count > 1 ? args.Parameters[1] : args.Player.Name;

                var targetPlayer = TShock.Players
                .Where(p =>
                {
                    if (p != null && p.Name.Equals(targetUsername, StringComparison.OrdinalIgnoreCase)) return true;
                    return false;
                })
                .FirstOrDefault();

                if (targetPlayer == null)
                {
                    args.Player.SendErrorMessage($"Player '{targetUsername}' not found.");
                    return;
                }
                

                if (!int.TryParse(args.Parameters[2], out int amount))
                {
                    args.Player.SendErrorMessage("Invalid amount.");
                    return;
                }

                Database.AddBalance(targetPlayer.Name, amount);
                args.Player.SendSuccessMessage($"Successfully gave {Utils.Util.CurrencyFormat(amount)} to {targetUsername}");
                #endregion
            }
            else
            {
                args.Player.SendErrorMessage("Usage: /admin <command>");
                args.Player.SendSuccessMessage("Command list:");
                args.Player.SendErrorMessage(usage);
            }
        }

        private static void SkynomiCmd(CommandArgs args)
        {
            string usage = "Usage: /skynomi <command>";

            var sb = new StringBuilder();
            sb.AppendLine("Command list:");
            sb.AppendLine("- cache <command/help> - Cache commands");

            if (args.Parameters.Count == 0)
            {
                args.Player.SendInfoMessage(usage);
            }
            else if (args.Parameters[0] == "help")
            {
                args.Player.SendInfoMessage(sb.ToString());
            }
            else if (args.Parameters[0] == "cache")
            {
                #region Cache
                string cacheUsage = "Usage: /skynomi cache <command/help>";
                if (args.Parameters.Count < 2)
                {
                    args.Player.SendInfoMessage(cacheUsage);
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
                    #endregion
                }
                else
                {
                    args.Player.SendErrorMessage(cacheUsage);
                }
                #endregion
            }
            else
            {
                args.Player.SendErrorMessage("Usage: /skynomi <command>");
            }
        }
    }
}
