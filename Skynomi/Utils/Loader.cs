using System.Reflection;
using Microsoft.Xna.Framework;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace Skynomi.Utils
{
    public static class Loader
    {
        public interface ISkynomiExtension
        {
            string Name { get; }
            string Description { get; }
            string Version { get; }
            string Author { get; }
            void Initialize();
        }

        public interface ISkynomiExtensionReloadable
        {
            void Reload(ReloadEventArgs args);
        }

        public interface ISkynomiExtensionDisposable
        {
            void Dispose();
        }

        public interface ISkynomiExtensionPostInit
        {
            void PostInitialize(EventArgs args);
        }


        private static List<ISkynomiExtension> _loadedExtensions = new List<ISkynomiExtension>();

        /// <summary>
        /// Load all Skynomi.xxx.dll extensions
        /// </summary>
        public static void Initialize()
        {
            Dispose();
            TShockAPI.Commands.ChatCommands.Add(new Command(Skynomi.Utils.Permissions.ListExtension, ListExtensionsCommand, "listextension", "le")
            {
                AllowServer = true,
                HelpText = "List all loaded extensions."
            });

            string[] pluginFiles = Directory.GetFiles(Path.Combine(TShock.SavePath, "..", "ServerPlugins"), "Skynomi.*.dll");

            foreach (string file in pluginFiles)
            {
                try
                {
                    Assembly assembly = Assembly.LoadFrom(file);
                    var types = assembly.GetTypes()
                        .Where(t => typeof(ISkynomiExtension).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    foreach (var type in types)
                    {
                        var instance = Activator.CreateInstance(type) as ISkynomiExtension;
                        if (instance != null)
                        {
                            _loadedExtensions.Add(instance);
                            instance.Initialize();
                            Console.WriteLine($"{Skynomi.Utils.Messages.Name} Info extension: {instance.Name} v{instance.Version} by {instance.Author}");
                        }
                        else
                        {
                            TShock.Log.ConsoleError($"{Skynomi.Utils.Messages.Name} Failed to create instance of {type.FullName}. Ensure it implements ISkynomiExtension correctly.");
                        }
                    }

                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleError($"{Skynomi.Utils.Messages.Name} Failed to load {Path.GetFileName(file)}: {ex.Message}");
                }
            }
        }

        private static void ListExtensionsCommand(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                if (_loadedExtensions.Count == 0)
                {
                    args.Player.SendInfoMessage("No extensions are currently loaded.");
                    return;
                }


                string text = "[c/00FF00:Loaded Extensions:]";
                int counter = 0;
                foreach (var extension in _loadedExtensions)
                {   counter++;
                    text += $"\n{counter}. [c/00AAFF:{extension.Name}] [c/FFFFFF:v{extension.Version}] by [c/AAAAAA:{extension.Author}]";
                }

                args.Player.SendMessage(text, Color.White);
            }
            else
            {
                string searchName = args.Parameters[0].ToLower();
                var extension = _loadedExtensions.FirstOrDefault(e => e.Name.ToLower().Contains(searchName));

                if (extension == null)
                {
                    args.Player.SendErrorMessage($"Extension '{searchName}' not found.");
                    return;
                }

                bool isReloadable = extension is ISkynomiExtensionReloadable;
                bool isDisposable = extension is ISkynomiExtensionDisposable;
                bool isPostInit = extension is ISkynomiExtensionPostInit;

                string detail = $"[c/00FF00:=== Extension Details ===]\n" +
                                $"[c/0000FF:Name:] [c/FFFFFF:{extension.Name}]\n" +
                                $"[c/0000FF:Description:] [c/FFFFFF:{extension.Description}]\n" +
                                $"[c/0000FF:Version:] [c/FFFFFF:{extension.Version}]\n" +
                                $"[c/0000FF:Author:] [c/FFFFFF:{extension.Author}]\n" +
                                $"[c/0000FF:Supports Reload:] [c/FFFFFF:{(isReloadable ? "Yes" : "No")}]\n" +
                                $"[c/0000FF:Supports Dispose:] [c/FFFFFF:{(isDisposable ? "Yes" : "No")}]\n" +
                                $"[c/0000FF:Supports PostInitialize:] [c/FFFFFF:{(isPostInit ? "Yes" : "No")}]";

                args.Player.SendMessage(detail, Color.White);
            }
        }

        /// <summary>
        /// Reload all loaded extensions
        /// </summary>
        public static void Reload(ReloadEventArgs args)
        {
            string text = Skynomi.Utils.Messages.Name + " Reloaded extensions:";
            bool isAvailable = false;
            foreach (var extension in _loadedExtensions)
            {
                try
                {
                    if (extension is ISkynomiExtensionReloadable reloadable)
                    {
                        reloadable.Reload(args);
                        text += $" \"{extension.Name}\"";
                        isAvailable = true;
                    }
                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleError($"{Skynomi.Utils.Messages.Name} Failed to reload {extension.Name}: {ex.Message}");
                }
            }

            if (isAvailable == true)
            {
                args.Player.SendSuccessMessage(text);
                return;
            }
        }

        public static void PostInitialize(EventArgs args)
        {
            string text = Skynomi.Utils.Messages.Name + " PostInitialized extensions:";
            bool isAvailable = false;
            foreach (var extension in _loadedExtensions)
            {
                try
                {
                    if (extension is ISkynomiExtensionPostInit postinitable)
                    {
                        postinitable.PostInitialize(args);
                        text += $" \"{extension.Name}\"";
                        isAvailable = true;
                    }
                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleError($"{Skynomi.Utils.Messages.Name} Failed to PostInitialize {extension.Name}: {ex.Message}");
                }
            }

            if (isAvailable == true)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(text);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Dispose all loaded extensions
        /// </summary>
        public static void Dispose()
        {
            string text = Skynomi.Utils.Messages.Name + " Disposed extensions:";
            bool isAvailable = false;
            foreach (var extension in _loadedExtensions)
            {
                try
                {
                    if (extension is ISkynomiExtensionDisposable disposable)
                    {
                        disposable.Dispose();
                        text += $" \"{extension.Name}\"";
                        isAvailable = true;
                    }
                }
                catch (Exception ex)
                {
                    TShock.Log.ConsoleError($"{Skynomi.Utils.Messages.Name} Failed to dispose {extension.Name}: {ex.Message}");
                }
            }

            if (isAvailable == true)
            {
                Console.WriteLine(text);
            }
            _loadedExtensions.Clear();
        }

        public static TerrariaPlugin GetPlugin()
        {
            return SkynomiPlugin.Instance;
        }
    }
}
