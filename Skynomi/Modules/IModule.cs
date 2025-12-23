namespace Skynomi.Modules;

public interface IModule
{
    string Name { get; }
    string Description { get; }
    Version Version { get; }
    string Author { get;  }
    void Initialize();
}