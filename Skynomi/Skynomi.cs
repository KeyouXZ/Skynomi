using System.Data;
using Microsoft.Data.Sqlite;
using TerrariaApi.Server;
using TShockAPI;
using Terraria;
using Microsoft.Xna.Framework;
using TShockAPI.Hooks;

namespace Skynomi
{
    [ApiVersion(2, 1)]
    public class SkynomiPlugin : TerrariaPlugin
    {
        public override string Author => "Keyou";
        public override string Description => "Terraria Economy System";
        public override string Name => "Skynomi";
        public override Version Version => new Version(1, 0, 0);

        private Config config;
        private SkyDatabase database;
        private SkyShop shop;
        private SqliteConnection _connection;

        // test
        private Dictionary<int, HashSet<string>> npcHitPlayers = new Dictionary<int, HashSet<string>>();
        private Dictionary<int, Dictionary<string, DateTime>> npcHitTimes = new Dictionary<int, Dictionary<string, DateTime>>();


        public SkynomiPlugin(Main game) : base(game)
        {
        }

        public override void Initialize()
        {   
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.NpcKilled.Register(this, OnNpcKilled);
            GeneralHooks.ReloadEvent += Reload;
            // test
            ServerApi.Hooks.NpcStrike.Register(this, OnNpcHit);

            Commands.ChatCommands.Add(new Command("skynomi.balance", Balance, "balance", "bal"));
            Commands.ChatCommands.Add(new Command("skynomi.pay", Pay, "pay"));
            SkyShop.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.NpcKilled.Deregister(this, OnNpcKilled);
                // test
                ServerApi.Hooks.NpcStrike.Deregister(this, OnNpcHit);

                _connection?.Close();
            }
            base.Dispose(disposing);
        }

        public void Reload(ReloadEventArgs args)
        {
            args.Player.SendMessage("Skynomi Reloaded", Color.LightGreen);
            config = Config.Read();
            SkyShop.Reload();
        }

        private void OnInitialize(EventArgs args)
        {
            config = Config.Read();
            SkyDatabase.InitializeDatabase();
        }

        private void OnNpcHit(NpcStrikeEventArgs args)
        {
            int npcId = args.Npc.whoAmI;
            TSPlayer player = TShock.Players[args.Player.whoAmI];
            string playerName = player.Name;


            // Ensure that the dictionary for NPC hits exists
            if (!npcHitPlayers.ContainsKey(npcId))
            {
                npcHitPlayers[npcId] = new HashSet<string>();
            }

            // Record the player who hit the NPC
            npcHitPlayers[npcId].Add(playerName);

            // Record the time of first hit for the player
            if (!npcHitTimes.ContainsKey(npcId))
            {
                npcHitTimes[npcId] = new Dictionary<string, DateTime>();
            }

            if (!npcHitTimes[npcId].ContainsKey(playerName))
            {
                npcHitTimes[npcId][playerName] = DateTime.UtcNow;
            }
        }

        private void OnNpcKilled(NpcKilledEventArgs args)
        {
            try
            {
                // Validate the last interaction index
                if (args.npc.lastInteraction < 0 || args.npc.lastInteraction >= TShock.Players.Length)
                    return;

                var killer = TShock.Players[args.npc.lastInteraction];
                if (killer == null || !killer.Active)
                    return;

                // Calculate base reward
                int baseReward = args.npc.boss ? (int)((args.npc.lifeMax / 4) * 0.5) : (int)((args.npc.lifeMax / 4) * 1.2);

                // Reward the killer
                SkyDatabase.AddBalance(killer.Name, baseReward);
                ShowFloatingText(killer, $"+{baseReward} {config.Currency}");

                // Reward other players who dealt damage
                if (npcHitPlayers.TryGetValue(args.npc.whoAmI, out HashSet<string> players))
                {
                    foreach (var playerName in players)
                    {
                        var player = TShock.Players.FirstOrDefault(p => p?.Name == playerName);
                        if (player != null && player.Active && player != killer)
                        {
                            if (!npcHitTimes.TryGetValue(args.npc.whoAmI, out Dictionary<string, DateTime> hitTimes) ||
                                !hitTimes.TryGetValue(playerName, out DateTime firstHitTime))
                            {
                                // Use the current time if no first hit time is found
                                firstHitTime = DateTime.UtcNow;
                            }

                            double timeElapsed = (DateTime.UtcNow - firstHitTime).TotalSeconds;

                            if (timeElapsed <= 30)
                            {
                                int rewardForPlayer = (int)(baseReward * 0.7);
                                SkyDatabase.AddBalance(player.Name, rewardForPlayer);
                                ShowFloatingText(player, $"+{rewardForPlayer} {config.Currency}");
                            }
                        }
                    }
                }

                // Clean up dictionaries
                npcHitTimes.Remove(args.npc.whoAmI);
                npcHitPlayers.Remove(args.npc.whoAmI);
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError(ex.ToString());
            }
        }

        private void ShowFloatingText(TSPlayer player, string text)
        {
            if (player?.Active != true)
                return;

            var position = player.TPlayer.position;
            position.Y -= 48f;

            player.SendData(PacketTypes.CreateCombatTextExtended, text, (int)Color.Blue.PackedValue, position.X, position.Y);
        }


        // Commands
        private void Balance(CommandArgs args) {
            // Login is required
            if (!args.Player.IsLoggedIn)
            {
                args.Player.SendErrorMessage("You must be logged in to use this commands");
                return;
            }

            if (args.Player == null)
                return;

            string targetUsername = args.Parameters.Count > 0 ? args.Parameters[0] : args.Player.Name;

            var targetPlayer = TShock.Players
            .Where(p => p != null && p.Name.Equals(targetUsername, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

            if (targetPlayer == null) {
                args.Player.SendErrorMessage($"Player '{targetUsername}' not found.");
                return;
            }

            decimal balance = SkyDatabase.GetBalance(targetPlayer.Name);

            if (args.Parameters.Count == 0) {
                targetUsername = "Your";
            } else {
                targetUsername = $"{targetPlayer.Name}'s";
            }

            args.Player.SendInfoMessage($"{targetUsername} balance: {balance} {config.Currency}");
        }

        private void Pay(CommandArgs args) {
            // Login is required
            if (!args.Player.IsLoggedIn) {
                args.Player.SendErrorMessage("You must be logged in to use this commands");
                return;
            }

            if (args.Player == null)
                return;

            string usage = "Usage: /pay <player> <amount>";
            // send usage message when only using /pay
            if (args.Parameters.Count == 0) {
                args.Player.SendErrorMessage(usage);
                return;
            }

            string targetUsername = args.Parameters.Count > 0 ? args.Parameters[0] : args.Player.Name;

            var targetPlayer = TShock.Players
            .Where(p => p != null && p.Name.Equals(targetUsername, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

            if (targetPlayer == null) {
                args.Player.SendErrorMessage($"Player '{targetUsername}' not found.");
                return;
            } else if (targetPlayer.Name == args.Player.Name) {
                args.Player.SendErrorMessage("You cannot pay yourself.");
                return;
            }

            decimal balancePlayer = SkyDatabase.GetBalance(args.Player.Name);
            decimal balanceTarget = SkyDatabase.GetBalance(targetPlayer.Name);


            // Check if the player has enough balance to pay
            if (!int.TryParse(args.Parameters[1], out int amount)) {
                args.Player.SendErrorMessage("Invalid amount.");
                return;
            }

            if (amount <= 0) {
                args.Player.SendErrorMessage("Amount must be greater than 0.");
                return;
            }

            if (balancePlayer < amount) {
                args.Player.SendErrorMessage($"You do not have enough {config.Currency} to pay.");
                return;
            }

            SkyDatabase.RemoveBalance(args.Player.Name, amount);
            SkyDatabase.AddBalance(targetPlayer.Name, amount);

            args.Player.SendInfoMessage($"You have paid {amount} {config.Currency} to {targetPlayer.Name}.");
            targetPlayer.SendInfoMessage($"You have received {amount} {config.Currency} from {args.Player.Name}.");
        }
    }
}