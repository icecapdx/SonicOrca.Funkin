using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using SonicOrca;
using SonicOrca.SDL2;

namespace SonicOrca.GameTemplate
{
    public static class Program
    {
        public static Version AppVersion = Assembly.GetExecutingAssembly().GetName().Version;
        public static string AppArchitecture = Environment.Is64BitProcess ? "x64" : "x86";
        public static Version AppMinOpenGLVersion = new Version(3, 3);

        internal const string IniConfigurationPath = "sonicorca.cfg";

        public static IniConfiguration Configuration { get; private set; }

        public static string UserDataDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "SonicOrca");

        internal static string LogPath => Path.Combine(UserDataDirectory, "sonicorca-gametemplate.log");

        public static IReadOnlyList<string> CommandLineArguments { get; private set; }

        private static void Main(string[] args)
        {
            CommandLineArguments = args;
            EnsureUserDataDirectoryExists();
            WriteLogHeader();
            LoadConfiguration();
            RunOrFocusGame();
        }

        private static void EnsureUserDataDirectoryExists()
        {
            if (!Directory.Exists(UserDataDirectory))
                Directory.CreateDirectory(UserDataDirectory);
        }

        private static void LoadConfiguration()
        {
            string path = Path.Combine(UserDataDirectory, IniConfigurationPath);
            if (File.Exists(path))
            {
                Configuration = new IniConfiguration(path);
            }
            else
            {
                Configuration = new IniConfiguration();
                Configuration.SetProperty("video", "fullscreen", "0");
                Configuration.SetProperty("audio", "volume", "1.0");
                Configuration.SetProperty("audio", "music_volume", "0.5");
                Configuration.SetProperty("audio", "sound_volume", "1.0");
                Configuration.Save(path);
            }
        }

        private static void RunOrFocusGame()
        {
            bool createdNew;
            using (new Mutex(true, "SonicOrca.GameTemplate", out createdNew))
            {
                if (createdNew || Configuration.GetPropertyBoolean("debug", "allow_multiple_instances"))
                    RunGame();
                else
                    FocusOtherInstance();
            }
        }

        private static IPlatform GetPlatform() => SDL2Platform.Instance;

        private static void RunGame()
        {
            using (IPlatform platform = GetPlatform())
            {
                try
                {
                    platform.Initialise();
                }
                catch (Exception ex)
                {
                    LogException(ex);
                    ShowErrorMessageBox(ex.Message);
                    return;
                }
                if (!CheckOpenGL(platform))
                    return;
                using (var context = new TemplateGameContext(platform))
                {
                    try
                    {
                        Trace.WriteLine("Initialising game template");
                        Trace.Indent();
                        context.Initialise();
                        Trace.Unindent();
                        Trace.WriteLine("Running game template (Escape to quit)");
                        Trace.Indent();
                        context.Run();
                    }
                    catch (Exception ex)
                    {
                        if (Debugger.IsAttached)
                            throw;
                        LogException(ex);
                        ShowErrorMessageBox(ex.Message);
                    }
                    finally
                    {
                        Trace.Unindent();
                    }
                }
            }
            Trace.Unindent();
            Trace.WriteLine(new string('-', 80));
            Trace.WriteLine(string.Empty);
        }

        private static void FocusOtherInstance()
        {
            Process current = Process.GetCurrentProcess();
            foreach (Process p in Process.GetProcessesByName(current.ProcessName))
            {
                if (p.Id != current.Id)
                {
#if WINDOWS_MESSAGE_BOX
                    WindowsShell.SetForegroundWindow(p.MainWindowHandle);
#endif
                    break;
                }
            }
        }

        private static bool CheckOpenGL(IPlatform platform)
        {
            Trace.WriteLine("Verifying OpenGL version");
            Version openGlVersion = platform.GetOpenGLVersion();
            Trace.WriteLine($"OpenGL {openGlVersion.Major}.{openGlVersion.Minor}");
            if (openGlVersion >= AppMinOpenGLVersion)
                return true;
            Trace.WriteLine("OpenGL version too low");
            ShowErrorMessageBox($"OpenGL {AppMinOpenGLVersion.Major}.{AppMinOpenGLVersion.Minor} or later is required.");
            return false;
        }

        public static void ShowErrorMessageBox(string text)
        {
            Trace.WriteLine("ERROR: " + text);
            Console.Error.WriteLine(text);
#if WINDOWS_MESSAGE_BOX
            WindowsShell.ShowMessageBox(text, "SonicOrca Game Template");
#endif
        }

        private static bool _traceListenersAdded;

        private static void WriteLogHeader()
        {
            Trace.AutoFlush = true;
            if (!_traceListenersAdded)
            {
                Trace.Listeners.Add(new TextWriterTraceListener(LogPath));
                Trace.Listeners.Add(new ConsoleTraceListener());
                _traceListenersAdded = true;
            }
            Trace.WriteLine(Environment.OSVersion);
            Trace.WriteLine($"SonicOrca.GameTemplate {AppVersion} [{AppArchitecture}]");
            Trace.WriteLine(DateTime.Now.ToString("dd MMMM yyyy @ hh:mm tt"));
            Trace.WriteLine(new string('-', 80));
            Trace.Indent();
        }

        public static void LogException(Exception ex, bool logStackTrace = true)
        {
            string[] messages = ex.Message.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (messages.Length <= 1)
                Trace.WriteLine("EXCEPTION: " + messages[0]);
            else
            {
                Trace.WriteLine("EXCEPTION:");
                Trace.Indent();
                foreach (string message in messages)
                    Trace.WriteLine(message);
                Trace.Unindent();
            }
            if (ex.InnerException != null)
            {
                Trace.Indent();
                LogException(ex.InnerException);
                Trace.Unindent();
            }
            if (!logStackTrace || string.IsNullOrEmpty(ex.StackTrace))
                return;
            string[] lines = ex.StackTrace.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            Trace.WriteLine("STACK TRACE:");
            Trace.Indent();
            foreach (string line in lines)
                Trace.WriteLine(line.Trim());
            Trace.Unindent();
        }
    }
}