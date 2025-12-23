namespace Skynomi.NpcLoot;

internal class NpcInteraction
{
    public Dictionary<string, int> DamageByPlayers { get; } = new();
    public Dictionary<string, DateTime> HitTimes { get; } = new();
}