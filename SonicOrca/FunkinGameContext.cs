using System.IO;
using System.Linq;
using System.Reflection;
using SonicOrca;
using SonicOrca.Core;
using SonicOrca.Drawing;
using SonicOrca.Drawing.LevelRendering;
using SonicOrca.Geometry;
using SonicOrca.Graphics;
using SonicOrca.Input;
using SonicOrca.Resources;
// Funkin
using SonicOrca.Funkin.Meta.State;

namespace SonicOrca.Funkin
{
    internal sealed class FunkinGameContext : SonicOrcaGameContext
    {
        private IFramebuffer _canvas;
        private FunkinGameSettings _settings;
        private IGameState _rootGameState;
        private Updater _gameStateUpdater;

        public FunkinGameContext(IPlatform platform) : base(platform)
        {
        }

        public override IAudioSettings AudioSettings => _settings;

        public override IVideoSettings VideoSettings => _settings;

        public override void Initialise()
        {
            base.Initialise();
            Configuration = Program.Configuration;
            UserDataDirectory = Program.UserDataDirectory;
            _canvas = Window.GraphicsContext.CreateFrameBuffer(1920, 1080);
            SonicOrcaGameContext.IsMaxPerformance = Configuration.GetPropertyBoolean("graphics", "max_performance");
            Audio.Volume = Configuration.GetPropertyDouble("audio", "volume", 1.0);
            Audio.MusicVolume = Configuration.GetPropertyDouble("audio", "music_volume", 0.5);
            Audio.SoundVolume = Configuration.GetPropertyDouble("audio", "sound_volume", 1.0);
            Input.IsVibrationEnabled = Configuration.GetPropertyBoolean("input", "vibration", true);
            _settings = new FunkinGameSettings(Configuration, Audio, Window);
            _settings.Apply();
            Window.WindowTitle = "Friday Night Funkin'";
            Window.AspectRatio = new Vector2i(16, 9);
            string contentRoot = GetContentRootDirectory();
            LoadResourceFiles(Path.Combine(contentRoot, "data"));
            if (bool.Parse(Configuration.GetProperty("general", "use_mods", "true")))
                LoadResourceFiles(Path.Combine(contentRoot, "mods"));
            _rootGameState = new PlayState(this);
            _gameStateUpdater = new Updater(_rootGameState.Update());
        }

        public override void Dispose()
        {
            _rootGameState?.Dispose();
            base.Dispose();
        }

        protected override void OnUpdate()
        {
            if (!Input.CurrentState.Keyboard[226] && !Input.CurrentState.Keyboard[230] || !Input.Pressed.Keyboard[40])
                return;
            Window.FullScreen = !Window.FullScreen;
        }

        protected override void OnUpdateStep()
        {
            Console.Update();
            NetworkManager.Update();
            if (!_gameStateUpdater.Update())
                Finish = true;
            if (Input.Pressed.Keyboard[41])
                Finish = true;
            foreach (Controller controller in Controllers)
                controller.Update();
            Input.OutputState.GamePad = Output.ToArray<GamePadOutputState>();
        }

        protected override void OnDraw()
        {
            I2dRenderer r2d = Renderer.Get2dRenderer();
            r2d.ClipRectangle = new Rectangle(0.0, 0.0, 1920.0, 1080.0);
            if (ForceHD)
                _canvas.Activate();
            else
                Window.GraphicsContext.RenderToBackBuffer();
            Window.GraphicsContext.ClearBuffer();
            _rootGameState.Draw();
            Renderer.DeativateRenderer();
            Console.Draw(Renderer);
            Renderer.DeativateRenderer();
            if (!ForceHD)
                return;
            Window.GraphicsContext.RenderToBackBuffer();
            r2d.BlendMode = BlendMode.Opaque;
            r2d.Colour = Colours.White;
            Vector2i clientSize = Window.ClientSize;
            r2d.ClipRectangle = new Rectangle(0.0, 0.0, clientSize.X, clientSize.Y);
            r2d.RenderTexture(_canvas.Textures[0], new Rectangle(0.0, 0.0, clientSize.X, clientSize.Y), flipy: true);
            r2d.Deactivate();
        }

        private static string GetContentRootDirectory()
        {
            string loc = Assembly.GetEntryAssembly()?.Location;
            if (string.IsNullOrEmpty(loc))
                loc = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(loc) ?? string.Empty;
        }

        private void LoadResourceFiles(string inputDirectory)
        {
            if (!Directory.Exists(inputDirectory))
                return;
            foreach (string file in Directory.GetFiles(inputDirectory, "*.dat", SearchOption.AllDirectories))
                ResourceTree.MergeWith(new ResourceFile(file).Scan());
        }

        protected override Renderer CreateRenderer() => new TheRenderer(Window);

        protected override ILevelRenderer CreateLevelRenderer(Level level) =>
            new LevelRenderer(level, VideoSettings);
    }
}
