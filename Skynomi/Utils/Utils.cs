using OTAPI;
using TShockAPI;

namespace Skynomi.Utils;

public class UtilsModule : Modules.IModule, Modules.IDisposable
{
    public string Name => "Utils";
    public string Description => "Utility helpers for login, permissions, currency formatting, and platform detection.";
    public Version Version => new(0, 1, 0);
    public string Author => "Keyou";

    public void Initialize()
    {
        Hooks.MessageBuffer.GetData += OnGetData;
    }

    public void Dispose()
    {
        Hooks.MessageBuffer.GetData -= OnGetData;
    }

    public bool CheckIfLogin(CommandArgs args)
    {
        if (args.Player.IsLoggedIn) return true;

        args.Player.SendErrorMessage(Messages.NotLogged);
        return false;
    }

    public bool CheckPermission(string perm, CommandArgs args)
    {
        if (args.Player.HasPermission(perm)) return true;

        args.Player.SendErrorMessage(Messages.PermissionError, Commands.Specifier);
        return false;
    }

    #region Currency

    public string CurrencyFormat(long amount)
    {
        string formattedAmount =
            SkynomiPlugin.SkynomiConfig.NumericAbbreviation ? FormatNumber(amount) : amount.ToString();
        return SkynomiPlugin.SkynomiConfig.CurrencyFormat.Replace("{currency}", SkynomiPlugin.SkynomiConfig.Currency)
            .Replace("{amount}", formattedAmount);
    }

    public string FormatNumber(long num)
    {
        return num switch
        {
            >= 1_000_000_000 => (num / 1_000_000_000D).ToString("0.#") + "B",
            >= 1_000_000 => (num / 1_000_000D).ToString("0.#") + "M",
            >= 1_000 => (num / 1_000D).ToString("0.#") + "K",
            _ => num.ToString()
        };
    }

    #endregion

    #region Platform

    // ReSharper disable UnusedMember.Local
    private enum PlatformType : byte
    {
        Mobile = 0,
        Stadia = 1,
        Xbox = 2,
        PlayStation = 3,
        Editor = 4,
        Switch = 5,
        // ReSharper disable once InconsistentNaming
        PC = 233
    }
    // ReSharper restore UnusedMember.Local

    private PlatformType[] Platforms { get; set; } = new PlatformType[256];

    /// <summary>
    ///     Gets the platform of the player.
    /// private enum PlatformType : byte
    /// {
    ///     Mobile = 0,
    ///     Stadia = 1,
    ///     Xbox = 2,
    ///     PlayStation = 3,
    ///     Editor = 4,
    ///     Switch = 5,
    ///     PC = 233
    /// }
    /// </summary>
    public string GetPlatform(int playerIndex)
    {
        return Platforms[playerIndex].ToString();
    }

    private void OnGetData(object? sender, Hooks.MessageBuffer.GetDataEventArgs e)
    {
        try
        {
            switch (e.MessageType)
            {
                case 1:
                    Platforms[e.Instance.whoAmI] = PlatformType.PC;
                    break;
                case 150:
                    e.Instance.ResetReader();
                    e.Instance.reader.BaseStream.Position = e.Start + 1;
                    var platform = e.Instance.reader.ReadByte();
                    Platforms[e.Instance.whoAmI] = (PlatformType)platform;

                    break;
            }
        }
        catch
        {
            // ignored
        }
    }

    #endregion
}