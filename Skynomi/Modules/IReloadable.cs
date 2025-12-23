using TShockAPI.Hooks;

namespace Skynomi.Modules;

public interface IReloadable
{
    void Reload(ReloadEventArgs args);
}