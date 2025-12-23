namespace Skynomi.Modules;

public interface IDependent
{
    IReadOnlyList<Type> RequiredModules { get; }
}