using TShockAPI;

namespace Skynomi.Utils
{
    public class Log
    {
        private static readonly ConsoleColor NameColor = ConsoleColor.Magenta;
        
        private static void CreateFolder() {
            try
            {
                if (!Directory.Exists(Skynomi.SkynomiPlugin.config.LogPath)) {
                    Directory.CreateDirectory(Skynomi.SkynomiPlugin.config.LogPath);
                }
            } catch (Exception ex) {
                Console.ForegroundColor = NameColor;
                Console.Write($"{Skynomi.Utils.Messages.Name} ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.ToString());
                Console.ResetColor();
            }
        }

        public static void LogFile(string message) {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm");
                CreateFolder();
                string lastBoot = SkynomiPlugin.timeBoot;
                var stackTrace = new System.Diagnostics.StackTrace();
                var frame = stackTrace.GetFrame(1)?.GetMethod();
                var methodName = frame?.Name ?? "Unknown";
                var className = frame?.ReflectedType?.Name ?? "Unknown";
                if (methodName != "General" && methodName != "Info" && methodName != "Error" && methodName != "Success" && methodName != "Warn") methodName = "Unknown";

                string msg =  timestamp + $" [{methodName.ToUpper().First()}]" + $" ({className})" + " => " + message + Environment.NewLine;

                File.AppendAllText(Path.Combine(TShock.SavePath, "Skynomi", "Skynomi.log"), msg);
                File.AppendAllText(Path.Combine(SkynomiPlugin.config.LogPath, $"{lastBoot}.log"), msg);
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.ToString());
                Console.ResetColor();
            }
        }

        public static void Info(string message) {
            CreateFolder();
            Console.ForegroundColor = NameColor;
            Console.Write($"{Skynomi.Utils.Messages.Name} ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(message);
            Console.ResetColor();
            LogFile(message);
        }

        public static void Error(string message) {
            CreateFolder();
            Console.ForegroundColor = NameColor;
            Console.Write($"{Skynomi.Utils.Messages.Name} ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
            LogFile(message);
        }

        public static void Success(string message) {
            CreateFolder();
            Console.ForegroundColor = NameColor;
            Console.Write($"{Skynomi.Utils.Messages.Name} ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
            LogFile(message);
        }

        public static void Warn(string message) {
            CreateFolder();
            Console.ForegroundColor = NameColor;
            Console.Write($"{Skynomi.Utils.Messages.Name} ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ResetColor();
            LogFile(message);
        }

        public static void General(string message) {
            CreateFolder();
            Console.ForegroundColor = NameColor;
            Console.Write($"{Skynomi.Utils.Messages.Name} ");
            Console.ResetColor();
            Console.WriteLine(message);
            LogFile(message);
        }
    }
}