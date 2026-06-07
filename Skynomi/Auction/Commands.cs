using Skynomi.Modules;
using TShockAPI;

namespace Skynomi.Auction;

public class Commands
{
    public static void Initialize()
    {
        TShockAPI.Commands.ChatCommands.Add(new Command(Permissions.Create, AuctionCmd, "auction"));
    }

    private static void AuctionCmd(CommandArgs args)
    {
        var utils = ModuleManager.Get<Utils.UtilsModule>();

        if (args.Parameters.Count == 0)
        {
            args.Player.SendInfoMessage("Usage: /auction <create/bid/list/info/cancel/claim/settings/admin>");
            return;
        }

        string sub = args.Parameters[0].ToLower();
        switch (sub)
        {
            case "create":
            {
                if (!utils.CheckPermission(Permissions.Create, args)) return;
                if (args.Parameters.Count < 2)
                {
                    args.Player.SendErrorMessage("Usage: /auction create <price>");
                    return;
                }

                if (!long.TryParse(args.Parameters[1], out long price) || price <= 0)
                {
                    args.Player.SendErrorMessage("Invalid price.");
                    return;
                }

                if (AuctionManager.AwaitingDrops.ContainsKey(args.Player.Name))
                {
                    args.Player.SendErrorMessage("You already have a pending auction creation. Drop the item!");
                    return;
                }

                AuctionManager.AwaitingDrops[args.Player.Name] = price;
                args.Player.SendSuccessMessage("Throw the item you want to auction!");
                break;
            }
            case "bid":
            {
                if (!utils.CheckPermission(Permissions.Bid, args)) return;
                if (args.Parameters.Count < 2)
                {
                    args.Player.SendErrorMessage("Usage: /auction bid <amount>");
                    return;
                }
                if (!long.TryParse(args.Parameters[1], out long amount) || amount <= 0)
                {
                    args.Player.SendErrorMessage("Invalid amount.");
                    return;
                }

                AuctionManager.Bid(args.Player, amount);
                break;
            }
            case "list":
            {
                var auctions = AuctionManager.GetActiveAuctions();
                if (auctions.Count == 0)
                {
                    args.Player.SendInfoMessage("No active auctions.");
                    return;
                }

                args.Player.SendInfoMessage("Active Auctions:");
                foreach (var a in auctions)
                {
                    args.Player.SendInfoMessage($"#{a.Id}: {a.SellerName} selling [i/s{a.Stack}:{a.ItemId}] - Current Bid: {utils.CurrencyFormat(a.CurrentBid)} (Ends in {a.EndTime - DateTimeOffset.UtcNow.ToUnixTimeSeconds()}s)");
                }
                break;
            }
            case "info":
            {
                if (args.Parameters.Count < 2)
                {
                    args.Player.SendErrorMessage("Usage: /auction info <id>");
                    return;
                }
                if (!int.TryParse(args.Parameters[1], out int id))
                {
                    args.Player.SendErrorMessage("Invalid ID.");
                    return;
                }
                var a = AuctionManager.GetAuction(id);
                if (a == null)
                {
                    args.Player.SendErrorMessage("Auction not found.");
                    return;
                }
                args.Player.SendInfoMessage($"Auction #{a.Id}");
                args.Player.SendInfoMessage($"Item: [i/s{a.Stack}:{a.ItemId}] (Prefix: {a.Prefix})");
                args.Player.SendInfoMessage($"Seller: {a.SellerName}");
                args.Player.SendInfoMessage($"Price: {utils.CurrencyFormat(a.StartingPrice)}");
                args.Player.SendInfoMessage($"High Bid: {utils.CurrencyFormat(a.CurrentBid)} by {(a.HighBidderName ?? "None")}");
                args.Player.SendInfoMessage($"Time Left: {a.EndTime - DateTimeOffset.UtcNow.ToUnixTimeSeconds()}s");
                break;
            }
            case "cancel":
            {
                if (!utils.CheckPermission(Permissions.Cancel, args)) return;
                if (args.Parameters.Count > 1 && int.TryParse(args.Parameters[1], out int id))
                {
                    AuctionManager.CancelAuction(args.Player, id);
                }
                else
                {
                    AuctionManager.CancelAuction(args.Player);
                }
                break;
            }
            case "claim":
            {
                if (!utils.CheckPermission(Permissions.Claim, args)) return;
                AuctionManager.Claim(args.Player);
                break;
            }
            case "settings":
            {
                if (!utils.CheckPermission(Permissions.Settings, args)) return;
                if (args.Parameters.Count < 3 || args.Parameters[1] != "broadcast")
                {
                    args.Player.SendErrorMessage("Usage: /auction settings broadcast <on/off>");
                    return;
                }

                var auction = ModuleManager.Get<Auction>();
                if (args.Parameters[2] == "on")
                    auction.AuctionConfig.BroadcastAuction = true;
                else if (args.Parameters[2] == "off")
                    auction.AuctionConfig.BroadcastAuction = false;
                else
                {
                    args.Player.SendErrorMessage("Invalid option.");
                    return;
                }

                args.Player.SendSuccessMessage($"Broadcast set to {auction.AuctionConfig.BroadcastAuction}");
                break;
            }
            case "admin":
            {
                if (!utils.CheckPermission(Permissions.Admin, args)) return;
                if (args.Parameters.Count < 2)
                {
                    args.Player.SendErrorMessage("Usage: /auction admin <cancel/end/list/reload>");
                    return;
                }

                string adminSub = args.Parameters[1].ToLower();
                if (adminSub == "cancel")
                {
                    if (args.Parameters.Count < 3 || !int.TryParse(args.Parameters[2], out int id))
                    {
                        args.Player.SendErrorMessage("Usage: /auction admin cancel <id>");
                        return;
                    }
                    AuctionManager.CancelAuction(args.Player, id);
                }
                else if (adminSub == "end")
                {
                    if (args.Parameters.Count < 3 || !int.TryParse(args.Parameters[2], out int id))
                    {
                        args.Player.SendErrorMessage("Usage: /auction admin end <id>");
                        return;
                    }
                    AuctionManager.ForceEndAuction(args.Player, id);
                }
                else if (adminSub == "list")
                {
                    var auctions = AuctionManager.GetActiveAuctions();
                    args.Player.SendInfoMessage($"Total Active: {auctions.Count}");
                    foreach (var a in auctions)
                        args.Player.SendInfoMessage($"#{a.Id}: {a.SellerName} - {a.ItemId}");
                }
                else if (adminSub == "reload")
                {
                    var auction = ModuleManager.Get<Auction>();
                    auction.AuctionConfig = Config.Read();
                    AuctionManager.Reload(auction.AuctionConfig);
                    args.Player.SendSuccessMessage("Auction config reloaded.");
                }
                break;
            }
            default:
                args.Player.SendErrorMessage("Invalid subcommand.");
                break;
        }
    }
}
