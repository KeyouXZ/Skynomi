using System.Globalization;
using System.Text;
using Microsoft.Xna.Framework;
using Skynomi.Modules;
using TShockAPI;

namespace Skynomi.Economy;

public abstract class Commands
{
    public static void Initialize()
    {
        var cmds = new[]
        {
            new
            {
                Perm = Permissions.Balance,
                Handler = (CommandDelegate)Balance,
                AllowServer = true,
                Help = "Displays the player's current currency balance.",
                Names = new[] { "balance", "bal" }
            },
            new
            {
                Perm = Permissions.Pay,
                Handler = (CommandDelegate)Pay,
                AllowServer = false,
                Help = "Allows a player to send currency to another player.",
                Names = new[] { "pay" }
            },
            new
            {
                Perm = Permissions.Leaderboard,
                Handler = (CommandDelegate)Leaderboard,
                AllowServer = true,
                Help = "Displays the server's currency leaderboard.",
                Names = new[] { "leaderboard", "lb" }
            }
        };


        foreach (var c in cmds)
            TShockAPI.Commands.ChatCommands.Add(
                new Command(
                    c.Perm,
                    c.Handler,
                    c.Names
                )
                {
                    AllowServer = c.AllowServer,
                    HelpText = c.Help
                }
            );
    }

    private static void Balance(CommandArgs args)
    {
        var economy = ModuleManager.Get<EconomyModule>();


        var player = args.Player;

        string uuid = string.IsNullOrEmpty(player.Account.Name) ? "server" : player.Account.Name;

        var walletBalance = economy.Db.GetWalletBalance(uuid);

        var utils = ModuleManager.Get<Utils.UtilsModule>();

        if (walletBalance != null)
        {
            player.SendSuccessMessage(
                $"Your balance\nMoney: {utils.CurrencyFormat(walletBalance.Value)}");
        }
        else
        {
            player.SendErrorMessage($"You do not have a balance.");
        }
    }

    private static void Pay(CommandArgs args)
    {
        if (args.Player == null)
            return;

        var economy = ModuleManager.Get<EconomyModule>();

        const string usage = "Usage: /pay <player> <amount>";

        // send usage message when only using /pay
        if (args.Parameters.Count == 0)
        {
            args.Player.SendErrorMessage(usage);
            return;
        }

        string targetUsername = args.Parameters.Count > 0 ? args.Parameters[0] : args.Player.Account.Name;

        var targetPlayer = TShock.Players
            .FirstOrDefault(p =>
                p != null && p.Account.Name.Equals(targetUsername, StringComparison.OrdinalIgnoreCase));

        if (targetPlayer == null)
        {
            var actualPlayer = TShock.Players.FirstOrDefault(p =>
                p != null && p.Name.Equals(targetUsername, StringComparison.OrdinalIgnoreCase));

            args.Player.SendErrorMessage(actualPlayer is not null
                ? $"Player {actualPlayer.Name} does not exist. Current active account: {actualPlayer.Account.Name}.\n"
                : $"Player '{targetUsername}' not found.");
            return;
        }

        if (targetPlayer.Account.Name == args.Player.Account.Name)
        {
            args.Player.SendErrorMessage("You cannot pay yourself.");
            return;
        }

        var balancePlayer = economy.Db.GetWalletBalance(args.Player.Account.Name);

        // Check if existed
        if (balancePlayer is null)
        {
            args.Player.SendErrorMessage("You do not have a balance.");
            return;
        }

        if (economy.Db.GetWalletBalance(targetPlayer.Account.Name) is null)
        {
            args.Player.SendErrorMessage($"{targetPlayer.Account.Name} does not have a balance.");
            return;
        }

        // Check if the player has enough balance to pay
        if (!long.TryParse(args.Parameters[1], out var amount))
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
            args.Player.SendErrorMessage($"You do not have enough {SkynomiPlugin.SkynomiConfig.Currency} to pay.");
            return;
        }

        economy.Db.UpdateWalletBalance(args.Player.Account.Name, x => x.Balance -= amount);

        economy.Db.UpdateWalletBalance(targetPlayer.Account.Name, x => x.Balance += amount);

        var utils = ModuleManager.Get<Utils.UtilsModule>();

        args.Player.SendInfoMessage($"You have paid {utils.CurrencyFormat(amount)} to {targetPlayer.Account.Name}.");
        targetPlayer.SendInfoMessage(
            $"You have received {utils.CurrencyFormat(amount)} from {args.Player.Account.Name}.");
    }

    private record Data(string? Name, long Amount);

    private static void Leaderboard(CommandArgs args)
    {
        int max = 10;
        string type = "Money";

        if (args.Parameters.Count > 0)
        {
            string inputType = args.Parameters[0].ToLowerInvariant();
            if (inputType is "money" or "bank")
            {
                type = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(inputType);
            }
            else
            {
                args.Player.SendErrorMessage("Leaderboard type must be 'Money' or 'Bank'.");
                return;
            }
        }

        if (args.Parameters.Count > 1 && int.TryParse(args.Parameters[1], out int top))
        {
            if (top > max)
            {
                args.Player.SendErrorMessage($"Max top is {max}");
                return;
            }

            max = top;
        }

        Data[] data;

        var db = ModuleManager.Get<Skynomi.Database.DatabaseModule>();

        if (type == "Money")
        {
            var balances = db.Db.GetCollection<Database.Wallet>().FindAll();
            data = balances.Select(x => new Data(x.Name, x.Balance))
                .OrderByDescending(x => x.Amount).ThenBy(x => x.Name)
                .Take(max).ToArray();
        }
        else // "Bank"
        {
            // var balances = db.Db.GetCollection<Database.BankAccount>().FindAll();
            // data = balances.Select(x => new Data(x.Username, x.Balance))
            //     .OrderByDescending(x => x.Amount).ThenBy(x => x.Name)
            //     .Take(max).ToArray();
            
            args.Player.SendErrorMessage("Under construction!");
            return;
        }

        var leaderboard = new StringBuilder();
        int topCount = Math.Min(data.Length, max);

        string title = $"Top {topCount} {type} Leaderboard";
        leaderboard.AppendLine(title);
        var util = ModuleManager.Get<Utils.UtilsModule>();

        int counter = 1;
        foreach (var d in data)
        {
            string name = string.IsNullOrEmpty(d.Name) ? "Unknown" : d.Name;
            string rankSymbol = counter switch
            {
                1 => $"[c/FFD700:1. {name}]",
                2 => $"[c/C0C0C0:2. {name}]",
                3 => $"[c/CD7F32:3. {name}]",
                _ => $"[c/FFFFFF:{counter}. {name}]"
            };

            string msg = $"{rankSymbol} - [c/808080:{util.FormatNumber(d.Amount)}]";

            if (counter == topCount)
                leaderboard.Append(msg);
            else
                leaderboard.AppendLine(msg);

            counter++;
        }

        args.Player.SendMessage(leaderboard.ToString(), Color.White);
    }
}