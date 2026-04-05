using SonicOrca;
using SonicOrca.Audio;
using SonicOrca.Geometry;
using SonicOrca.Graphics;

namespace SonicOrca.Funkin
{
    internal sealed class FunkinGameSettings : IAudioSettings, IVideoSettings
    {
        private readonly AudioContext _audioContext;
        private readonly WindowContext _windowContext;
        private readonly IniConfiguration _config;
        private VideoMode _mode;
        private bool _enableShadows;
        private bool _enableWaterEffects;
        private bool _enableHeatEffects;

        public double MusicVolume
        {
            get => _audioContext.MusicVolume;
            set
            {
                _audioContext.MusicVolume = value;
                _config.SetProperty("audio", "music_volume", value.ToString().ToLowerInvariant());
                _config.Save();
            }
        }

        public double SoundVolume
        {
            get => _audioContext.SoundVolume;
            set
            {
                _audioContext.SoundVolume = value;
                _config.SetProperty("audio", "sound_volume", value.ToString().ToLowerInvariant());
                _config.Save();
            }
        }

        public VideoMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                _config.SetProperty("video", "fullscreen", ((int)_mode).ToString().ToLowerInvariant());
                _config.Save();
            }
        }

        public Resolution Resolution { get; set; }

        public bool EnableShadows
        {
            get => _enableShadows;
            set
            {
                _enableShadows = value;
                _config.SetProperty("graphics", "shadows", value.ToString().ToLowerInvariant());
                _config.Save();
            }
        }

        public bool EnableWaterEffects
        {
            get => _enableWaterEffects;
            set
            {
                _enableWaterEffects = value;
                _config.SetProperty("graphics", "water_effects", value.ToString().ToLowerInvariant());
                _config.Save();
            }
        }

        public bool EnableHeatEffects
        {
            get => _enableHeatEffects;
            set
            {
                _enableHeatEffects = value;
                _config.SetProperty("graphics", "heat_effects", value.ToString().ToLowerInvariant());
                _config.Save();
            }
        }

        public FunkinGameSettings(IniConfiguration config, AudioContext audioContext, WindowContext windowContext)
        {
            _config = config;
            _audioContext = audioContext;
            _windowContext = windowContext;
            _audioContext.MusicVolume = config.GetPropertyDouble("audio", "music_volume", 1.0);
            _audioContext.SoundVolume = config.GetPropertyDouble("audio", "sound_volume", 1.0);
            _mode = VideoMode.Windowed;
            if (int.TryParse(config.GetProperty("video", "fullscreen", "0"), out int fullscreen))
                _mode = (VideoMode)fullscreen;
            int rw = 1280;
            int rh = 720;
            if (int.TryParse(config.GetProperty("video", "width"), out int cw) && cw > 0)
                rw = cw;
            if (int.TryParse(config.GetProperty("video", "height"), out int ch) && ch > 0)
                rh = ch;
            Resolution = new Resolution(rw, rh);
            _enableShadows = config.GetPropertyBoolean("graphics", "shadows", true);
            _enableWaterEffects = config.GetPropertyBoolean("graphics", "water_effects", true);
            _enableHeatEffects = config.GetPropertyBoolean("graphics", "heat_effects");
        }

        public void Apply()
        {
            _windowContext.Mode = _mode;
            if (_mode == VideoMode.Windowed)
                _windowContext.ClientSize = new Vector2i(Resolution.Width, Resolution.Height);
        }
    }
}
