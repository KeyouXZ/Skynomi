using System.Data;
using Microsoft.Xna.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace Skynomi.NpcLoot;

public class NpcLootModule : Modules.IModule, Modules.IReloadable, Modules.IDisposable, Modules.IDependent
{
    public string Name => "NpcLoot";
    public string Description => "Get balance when killing enemy";
    public Version Version => new(0, 1, 0);

    public IReadOnlyList<Type> RequiredModules =>
    [
        typeof(Skynomi.Utils.UtilsModule), typeof(Database.DatabaseModule), typeof(Economy.EconomyModule)
    ];

    public string Author => "Keyou";

    public Config NpcLootConfig = null!;
    private readonly Dictionary<int, NpcInteraction> _npcInteractions = new();

    public void Initialize()
    {
        NpcLootConfig = Config.Read();

        GetDataHandlers.KillMe += PlayerDead;
        ServerApi.Hooks.NpcKilled.Register(SkynomiPlugin.Instance, OnNpcKilled);
        ServerApi.Hooks.NpcStrike.Register(SkynomiPlugin.Instance, OnNpcHit);
    }

    public void Reload(ReloadEventArgs e)
    {
        NpcLootConfig = Config.Read();
    }

    public void Dispose()
    {
        GetDataHandlers.KillMe += PlayerDead;
        ServerApi.Hooks.NpcKilled.Deregister(SkynomiPlugin.Instance, OnNpcKilled);
        ServerApi.Hooks.NpcStrike.Deregister(SkynomiPlugin.Instance, OnNpcHit);
    }

    private void OnNpcHit(NpcStrikeEventArgs args)
    {
        int npcId = args.Npc.whoAmI;
        var player = TShock.Players[args.Player.whoAmI];
        if (!player.IsLoggedIn) return;

        // DEBUG
        Log.Debug($"{player.Account.Name} hit!");

        // Blacklist check
        if (NpcLootConfig.BlacklistNpc.Contains(args.Npc.netID))
            return;

        string playerName = player.Account.Name;

        if (!_npcInteractions.TryGetValue(npcId, out var interaction))
        {
            interaction = new NpcInteraction();
            _npcInteractions[npcId] = interaction;
        }

        interaction.DamageByPlayers.TryAdd(playerName, 0);

        int cappedDamage = Math.Min(args.Damage, args.Npc.life);
        interaction.DamageByPlayers[playerName] += cappedDamage;

        if (!interaction.HitTimes.ContainsKey(playerName))
            interaction.HitTimes[playerName] = DateTime.UtcNow;
    }

    private readonly Dictionary<string, long> _rewardAccumulator = new();
    private readonly Dictionary<string, HashSet<string>> _rewardSources = new();
    private readonly HashSet<string> _flushScheduled = [];
    private readonly Lock _lock = new();

    private void OnNpcKilled(NpcKilledEventArgs args)
    {
        try
        {
            if (args.npc.lastInteraction < 0 || args.npc.lastInteraction >= TShock.Players.Length)
                return;

            // Blacklist check
            if (NpcLootConfig.BlacklistNpc.Contains(args.npc.netID))
                return;

            var killer = TShock.Players[args.npc.lastInteraction];
            if (killer is { IsLoggedIn: false } or null) return;

            if (args.npc.SpawnedFromStatue && !NpcLootConfig.RewardFromStatue ||
                (args.npc.friendly || args.npc.CountsAsACritter) && !NpcLootConfig.RewardFromFriendlyNpc)
            {
                _npcInteractions.Remove(args.npc.whoAmI);
                return;
            }

            string rewardFormula = args.npc.boss ? NpcLootConfig.BossReward : NpcLootConfig.NpcReward;

            rewardFormula = rewardFormula.Replace("{hp}", args.npc.lifeMax.ToString());

            int baseReward;
            try
            {
                var result = new DataTable().Compute(rewardFormula, null);
                baseReward = Convert.ToInt32(result);
            }
            catch
            {
                Log.Error($"Invalid reward formula: {rewardFormula}");
                return;
            }

            if (!_npcInteractions.TryGetValue(args.npc.whoAmI, out var interaction)) return;

            int totalDamage = interaction.DamageByPlayers.Values.Sum();
            if (totalDamage == 0) return;

            Random random = new();

            foreach (var (playerName, playerDamage) in interaction.DamageByPlayers)
            {
                double damagePercentage = (double)playerDamage / totalDamage; // Percentage of total damage
                long playerReward = (long)(baseReward * damagePercentage);

                double chance = random.NextDouble() * 100;
                if (chance > NpcLootConfig.RewardChance)
                {
                    continue;
                }

                if (playerName == killer.Account.Name)
                {
                    playerReward += (int)(baseReward * 0.1); // Bonus 10%
                }

                var player = TShock.Players.FirstOrDefault(p => p?.Account.Name == playerName);
                if (player is { IsLoggedIn: false } or null) continue;
                if (playerReward <= 0) continue;

                string source =
                    NPC.GetFullnameByID(args.npc.netID);

                lock (_lock)
                {
                    _rewardAccumulator[player.Account.Name] =
                        _rewardAccumulator.GetValueOrDefault(player.Account.Name) + playerReward;

                    if (!_rewardSources.TryGetValue(player.Account.Name, out var sources))
                    {
                        sources = _rewardSources[player.Account.Name] = [];
                    }

                    sources.Add(source);

                    // Schedule flush if not already scheduled
                    if (!_flushScheduled.Add(player.Account.Name)) continue;

                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(500);
                        await FlushAccumulatedReward(playerName, player);
                    });
                }
            }

            _npcInteractions.Remove(args.npc.whoAmI);
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
        }
    }

    private Task FlushAccumulatedReward(string playerName, TSPlayer player)
    {
        Log.Debug("Accumulator called!");

        long total;
        HashSet<string> sources;
        bool valid;

        lock (_lock)
        {
            if (!_flushScheduled.Remove(playerName)) return Task.CompletedTask;

            total = _rewardAccumulator.GetValueOrDefault(playerName);
            sources = _rewardSources.GetValueOrDefault(playerName, []);
            _rewardAccumulator.Remove(playerName);
            _rewardSources.Remove(playerName);
            valid = total > 0 && player.IsLoggedIn && player.Account.Name == playerName;
        }

        if (!valid)
        {
            Log.Debug($"Accumulator not valid!");
            return Task.CompletedTask;
        }

        Log.Debug(
            $"Flush check: total={total}, isLoggedIn={player.IsLoggedIn}, nameMatch={player.Account.Name == playerName}");

        var combinedSource = string.Join(", ", sources.Take(3));
        if (sources.Count > 3) combinedSource += ", +more";

        var economy = Modules.ModuleManager.Get<Economy.EconomyModule>();

        try
        {
            economy.Db.UpdateWalletBalance(player.Account.Name, x => x.Balance += total);
            Utils.ShowFloatingText(player, total, combinedSource);
        }
        catch (Exception ex)
        {
            Log.Error($"Accumulator error: {ex}");
        }

        return Task.CompletedTask;
    }

    private void PlayerDead(object? sender, GetDataHandlers.KillMeEventArgs args)
    {
        if (!args.Player.IsLoggedIn || NpcLootConfig.DropOnDeath <= 0) return;

        var economy = Modules.ModuleManager.Get<Economy.EconomyModule>();
        var utils = Modules.ModuleManager.Get<Skynomi.Utils.UtilsModule>();

        var playerBalance = economy.Db.GetWalletBalance(args.Player.Account.Name);
        if (playerBalance is null) return;

        var toLose = (long)(playerBalance.Value * (NpcLootConfig.DropOnDeath / 100));
        economy.Db.UpdateWalletBalance(args.Player.Account.Name, x => x.Balance -= toLose);

        args.Player.SendMessage($"You lost {utils.CurrencyFormat(toLose)} from dying!", Color.Orange);
    }
}