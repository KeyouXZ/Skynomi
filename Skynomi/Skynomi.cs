using TerrariaApi.Server;
using TShockAPI;
using Terraria;
using Microsoft.Xna.Framework;
using TShockAPI.Hooks;
using System.Data;

namespace Skynomi
{
    [ApiVersion(2, 1)]
    public class SkynomiPlugin : TerrariaPlugin
    {
        public override string Author => "Keyou";
        public override string Description => "Terraria Economy System";
        public override string Name => "Skynomi";
        public override Version Version => new Version(1, 0, 2);

        private Skynomi.Config config;
        private Skynomi.Database.Database database;

        private Dictionary<int, NpcInteraction> npcInteractions = new Dictionary<int, NpcInteraction>();

        public SkynomiPlugin(Main game) : base(game)
        {
        }

        public override void Initialize()
        {
            config = Config.Read();
            database = new Skynomi.Database.Database();
            database.InitializeDatabase();

            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
            ServerApi.Hooks.NpcKilled.Register(this, OnNpcKilled);
            ServerApi.Hooks.NpcStrike.Register(this, OnNpcHit);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnPlayerJoin);
            GeneralHooks.ReloadEvent += Reload;
            GetDataHandlers.KillMe += PlayerDead;

            Skynomi.ShopSystem.Shop.Initialize();
            Skynomi.Commands.Initialize();
            Skynomi.RankSystem.Ranks.Initialize();
            Skynomi.Utils.Util.Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NpcKilled.Deregister(this, OnNpcKilled);
                ServerApi.Hooks.NpcStrike.Deregister(this, OnNpcHit);
                GeneralHooks.ReloadEvent -= Reload;
                GetDataHandlers.KillMe -= PlayerDead;
                
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
            
            Skynomi.ShopSystem.Shop.Reload();
            Skynomi.Commands.Reload();
            Skynomi.RankSystem.Ranks.Reload();
            Skynomi.Utils.Util.Reload();

            args.Player.SendSuccessMessage(Skynomi.Utils.Messages.Reload);
        }

        private void OnPostInitialize(EventArgs args)
        {
            Skynomi.Database.Database.PostInitialize();
            Skynomi.ShopSystem.Shop.PostInitialize();
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
                if (args.npc.friendly && !config.RewardFromFriendlyNPC) return;

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
                    TShock.Log.ConsoleError($"Invalid reward formula: {rewardFormula}");
                    return;
                }

                if (!npcInteractions.TryGetValue(args.npc.whoAmI, out var interaction)) return;

                int totalDamage = interaction.DamageByPlayers.Values.Sum();
                if (totalDamage == 0) return;

                foreach (var (playerName, playerDamage) in interaction.DamageByPlayers)
                {
                    double damagePercentage = (double)playerDamage / totalDamage; // Percentage of total damage
                    int playerReward = (int)(baseReward * damagePercentage);

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
                            ShowFloatingText(player, new string[] { "[Skynomi]", $"+ {Skynomi.Utils.Util.CurrencyFormat(playerReward)}", $"From: {NPC.GetFullnameByID(args.npc.netID)}", $"+ {Skynomi.Utils.Util.CurrencyFormat(playerReward)}" });
                        }
                    }
                }

                npcInteractions.Remove(args.npc.whoAmI);
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError(ex.ToString());
            }
        }


        public void PlayerDead(object sender, GetDataHandlers.KillMeEventArgs args)
        {
            if (args.Player.IsLoggedIn && config.DropOnDeath > 0)
            {

                decimal playerBalance = database.GetBalance(args.Player.Name);
                var toLose = (int)(playerBalance * (config.DropOnDeath / 100));
                database.RemoveBalance(args.Player.Name, toLose);
                args.Player.SendMessage($"You lost {Skynomi.Utils.Util.CurrencyFormat(toLose)} from dying!", Color.Orange);
                return;
            }
            else
            {
                return;
            }
        }


        private void ShowFloatingText(TSPlayer player, string[] texts)
        {
            if (player?.Active != true)
                return;

            var position = player.TPlayer.position;

            if (config.Theme.ToLower() == "detailed")
            {
                var orangeColor = new Color(255, 165, 0);
                var greenColor = new Color(0, 255, 0);
                var blueColor = new Color(0, 0, 255);
                float posX = player.X + 400;

                player.SendData(PacketTypes.CreateCombatTextExtended, texts[0], (int)orangeColor.PackedValue, posX, player.Y + 12);
                player.SendData(PacketTypes.CreateCombatTextExtended, texts[1], (int)greenColor.PackedValue, posX, player.Y + 32);
                player.SendData(PacketTypes.CreateCombatTextExtended, texts[2], (int)blueColor.PackedValue, posX, player.Y + 52);
                return;
            }
            else
            {
                position.Y -= 48f;
                player.SendData(PacketTypes.CreateCombatTextExtended, texts[3], (int)Color.Blue.PackedValue, position.X, position.Y);
                return;
            } 

        }
    }

    public class NpcInteraction
    {
        public HashSet<string> HitPlayers { get; set; } = new HashSet<string>();
        public Dictionary<string, int> DamageByPlayers { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, DateTime> HitTimes { get; set; } = new Dictionary<string, DateTime>();
    }
}
