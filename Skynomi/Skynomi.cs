﻿using TerrariaApi.Server;
using TShockAPI;
using Terraria;
using Microsoft.Xna.Framework;
using TShockAPI.Hooks;
using System.Data;
using System.Text;

namespace Skynomi
{
    [ApiVersion(2, 1)]
    public class SkynomiPlugin : TerrariaPlugin
    {
        public override string Author => "Keyou";
        public override string Description => "Terraria Economy System";
        public override string Name => "Skynomi";
        public override Version Version => new Version(3, 1, 0);

        public static Skynomi.Config config;
        private Skynomi.Database.Database database;

        private Dictionary<int, NpcInteraction> npcInteractions = new Dictionary<int, NpcInteraction>();
        public static string timeBoot;

        public static SkynomiPlugin Instance { get; private set; }
        
        public SkynomiPlugin(Main game) : base(game)
        {
            Instance = this;
        }

        public override void Initialize()
        {
            timeBoot = DateTime.Now.ToString("yyyy-MM-dd-HH-mm");
            config = Config.Read();
            database = new Skynomi.Database.Database();
            database.InitializeDatabase();
            database.BalanceInitialize();

            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
            ServerApi.Hooks.NpcKilled.Register(this, OnNpcKilled);
            ServerApi.Hooks.NpcStrike.Register(this, OnNpcHit);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnPlayerJoin);
            GeneralHooks.ReloadEvent += Reload;
            GetDataHandlers.KillMe += PlayerDead;

            Skynomi.Commands.Initialize();
            Skynomi.Utils.Util.Initialize();

            // Extension
            Skynomi.Utils.Loader.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Extension
                Skynomi.Utils.Loader.Dispose();
                
                ServerApi.Hooks.NpcKilled.Deregister(this, OnNpcKilled);
                ServerApi.Hooks.NpcStrike.Deregister(this, OnNpcHit);
                GeneralHooks.ReloadEvent -= Reload;
                GetDataHandlers.KillMe -= PlayerDead;

                Skynomi.Utils.Log.General(Skynomi.Utils.Messages.CacheSaving);
                Skynomi.Database.CacheManager.StopAutoSave();
                Skynomi.Database.CacheManager.SaveAll();
                Skynomi.Utils.Log.Info(Skynomi.Utils.Messages.CacheSaved);
                Skynomi.Database.Database.Close();

            }
            base.Dispose(disposing);
        }

        public void Reload(ReloadEventArgs args)
        {
            config = Config.Read();

            Skynomi.Database.Database.Close();
            database = new Skynomi.Database.Database();
            database.InitializeDatabase();
            Skynomi.Database.Database.PostInitialize();

            Skynomi.Commands.Reload();
            Skynomi.Utils.Util.Reload();

            // Extension
            Skynomi.Utils.Loader.Reload(args);

            args.Player.SendSuccessMessage(Skynomi.Utils.Messages.Reload);
        }

        private void OnPostInitialize(EventArgs args)
        {
            Skynomi.Database.Database.PostInitialize();
            
            // Extension
            Skynomi.Utils.Loader.PostInitialize(args);
        }

        private void OnPlayerJoin(GreetPlayerEventArgs args)
        {
            database.CreatePlayer(TShock.Players[args.Who].Name);
        }

        private void OnNpcHit(NpcStrikeEventArgs args)
        {
            int npcId = args.Npc.whoAmI;
            TSPlayer player = TShock.Players[args.Player.whoAmI];
            if (player == null) return;

            string playerName = player.Name;

            if (!npcInteractions.TryGetValue(npcId, out var interaction))
            {
                interaction = new NpcInteraction();
                npcInteractions[npcId] = interaction;
            }

            interaction.HitPlayers.Add(playerName);

            if (!interaction.DamageByPlayers.ContainsKey(playerName))
                interaction.DamageByPlayers[playerName] = 0;

            int cappedDamage = Math.Min(args.Damage, args.Npc.life);
            interaction.DamageByPlayers[playerName] += cappedDamage;

            if (!interaction.HitTimes.ContainsKey(playerName))
                interaction.HitTimes[playerName] = DateTime.UtcNow;
        }

        private void OnNpcKilled(NpcKilledEventArgs args)
        {
            try
            {
                if (args.npc.lastInteraction < 0 || args.npc.lastInteraction >= TShock.Players.Length)
                    return;

                var killer = TShock.Players[args.npc.lastInteraction];
                if (killer == null || !killer.Active)
                    return;

                if (args.npc.SpawnedFromStatue && !config.RewardFromStatue) return;
                if (args.npc.friendly && !config.RewardFromFriendlyNpc) return;

                string rewardFormula = args.npc.boss ? config.BossReward : config.NpcReward;

                rewardFormula = rewardFormula.Replace("{hp}", args.npc.lifeMax.ToString());

                int baseReward;
                try
                {
                    var result = new DataTable().Compute(rewardFormula, null);
                    baseReward = Convert.ToInt32(result);
                }
                catch
                {
                    Skynomi.Utils.Log.Error($"Invalid reward formula: {rewardFormula}");
                    return;
                }

                if (!npcInteractions.TryGetValue(args.npc.whoAmI, out var interaction)) return;

                int totalDamage = interaction.DamageByPlayers.Values.Sum();
                if (totalDamage == 0) return;

                Random random = new Random();

                foreach (var (playerName, playerDamage) in interaction.DamageByPlayers)
                {
                    double damagePercentage = (double)playerDamage / totalDamage; // Percentage of total damage
                    long playerReward = (long)(baseReward * damagePercentage);

                    double chance = random.NextDouble() * 100;
                    if (chance > config.RewardChance)
                    {
                        continue;
                    }

                    if (playerName == killer.Name)
                    {
                        playerReward += (int)(baseReward * 0.1); // Bonus 10%
                    }

                    var player = TShock.Players.FirstOrDefault(p => p?.Name == playerName);
                    if (player != null && player.Active)
                    {
                        if (playerReward > 0)
                        {
                            database.AddBalance(player.Name, playerReward);
                            ShowFloatingText(player, playerReward, NPC.GetFullnameByID(args.npc.netID));
                        }
                    }
                }

                npcInteractions.Remove(args.npc.whoAmI);
            }
            catch (Exception ex)
            {
                Skynomi.Utils.Log.Error(ex.ToString());
            }
        }


        public void PlayerDead(object sender, GetDataHandlers.KillMeEventArgs args)
        {
            if (args.Player.IsLoggedIn && config.DropOnDeath > 0)
            {

                long playerBalance = database.GetBalance(args.Player.Name);
                var toLose = (long)(playerBalance * (config.DropOnDeath / 100));
                database.RemoveBalance(args.Player.Name, toLose);
                args.Player.SendMessage($"You lost {Skynomi.Utils.Util.CurrencyFormat(toLose)} from dying!", Color.Orange);
                return;
            }
            else
            {
                return;
            }
        }

        #region Floating Text
        private CancellationTokenSource? floatingTextCancelToken;
        private void ShowFloatingText(TSPlayer player, long amount, string from)
        {
            if (player?.Active != true)
                return;

            long balance = database.GetBalance(player.Name);

            var position = player.TPlayer.position;

            if (Skynomi.Utils.Util.GetPlatform(player) == "PC")
            {
                floatingTextCancelToken?.Cancel();
                floatingTextCancelToken?.Dispose();
                floatingTextCancelToken = new CancellationTokenSource();

                string message = string.Format("{5}[c/ff9900:[Skynomi][c/ff9900:]]{6}\r\n{0}{1}\r\n{2}\r\n[c/ffff00:Bal:] {3}{4}",
                "+",
                Skynomi.Utils.Util.CurrencyFormat(amount).ToString(),
                $"[c/00ff26:for {from}]" + RepeatEmptySpaces(100),
                Skynomi.Utils.Util.CurrencyFormat(balance).ToString(),
                RepeatLineBreaks(69),
                RepeatLineBreaks(1),
                RepeatEmptySpaces(100));

                player.SendData(PacketTypes.Status, message, 0);

                Task.Delay(5000, floatingTextCancelToken.Token).ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                    {
                        player.SendData(PacketTypes.Status, "", 0);
                    }
                }, TaskScheduler.Default);
            }
            else
            {
                position.Y -= 48f;
                string text = $"+ {Skynomi.Utils.Util.CurrencyFormat(amount)}";
                player.SendData(PacketTypes.CreateCombatTextExtended, text, (int)Color.Blue.PackedValue, position.X, position.Y);
                return;
            }

        }

        protected string RepeatLineBreaks(int number)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < number; i++)
            {
                sb.Append("\r\n");
            }

            return sb.ToString();
        }

        protected string RepeatEmptySpaces(int number)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < number; i++)
            {
                sb.Append(" ");
            }

            return sb.ToString();
        }
        #endregion
    }

    public class NpcInteraction
    {
        public HashSet<string> HitPlayers { get; set; } = new HashSet<string>();
        public Dictionary<string, int> DamageByPlayers { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, DateTime> HitTimes { get; set; } = new Dictionary<string, DateTime>();
    }
}
