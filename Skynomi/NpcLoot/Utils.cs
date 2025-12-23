using System.Text;
using Microsoft.Xna.Framework;
using Skynomi.Modules;
using TShockAPI;

namespace Skynomi.NpcLoot;

public abstract class Utils
{
    private static CancellationTokenSource? _floatingTextCancelToken;

    public static void ShowFloatingText(TSPlayer player, long amount, string from)
    {
        if (!player.IsLoggedIn)
            return;

        var economy = ModuleManager.Get<Economy.EconomyModule>();
        var utils = ModuleManager.Get<Skynomi.Utils.UtilsModule>();

        var balance = economy.Db.GetWalletBalance(player.Account.Name);
        if (balance is null) return;

        var position = player.TPlayer.position;

        if (utils.GetPlatform(player.Index) == "PC")
        {
            _floatingTextCancelToken?.Cancel();
            _floatingTextCancelToken?.Dispose();
            _floatingTextCancelToken = new CancellationTokenSource();

            string message = string.Format(
                "{5}[c/ff9900:[Skynomi][c/ff9900:]]{6}\r\n{0}{1}\r\n{2}\r\n[c/ffff00:Bal:] {3}{4}",
                "+",
                utils.CurrencyFormat(amount),
                $"[c/00ff26:for {from}]" + RepeatEmptySpaces(100),
                utils.CurrencyFormat(balance.Value),
                RepeatLineBreaks(69),
                RepeatLineBreaks(1),
                RepeatEmptySpaces(100));

            player.SendData(PacketTypes.Status, message);

            Task.Delay(5000, _floatingTextCancelToken.Token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                {
                    player.SendData(PacketTypes.Status);
                }
            }, TaskScheduler.Default);
        }
        else
        {
            position.Y -= 48f;
            string text = $"+ {utils.CurrencyFormat(amount)}";
            player.SendData(PacketTypes.CreateCombatTextExtended, text, (int)Color.Blue.PackedValue, position.X,
                position.Y);
        }
        
        Log.Debug("Floating text called!");
    }

    private static string RepeatLineBreaks(int number)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < number; i++)
        {
            sb.Append("\r\n");
        }

        return sb.ToString();
    }

    private static string RepeatEmptySpaces(int number)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < number; i++)
        {
            sb.Append(' ');
        }

        return sb.ToString();
    }
}