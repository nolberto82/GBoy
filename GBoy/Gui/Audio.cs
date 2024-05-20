using Raylib_cs;

namespace GBoy.Gui
{
    public class Audio
    {
        public static AudioStream Stream { get; private set; }

        public Audio() 
        {
            Raylib.InitAudioDevice();
            Raylib.SetAudioStreamBufferSizeDefault(Apu.MaxSamples);
            Stream = Raylib.LoadAudioStream(Apu.SampleRate, 8, 2);
            SetVolume(0.5f);
            Raylib.SetMasterVolume(0.25f);
            Raylib.PlayAudioStream(Stream);
        }

        public static void SetVolume(float v) => Raylib.SetMasterVolume(v);
        public static void Update(byte[] AudioBuffer)
        {
            unsafe
            {
                if (Raylib.IsAudioStreamProcessed(Stream))
                {
                    fixed (void* ptr = AudioBuffer)
                        Raylib.UpdateAudioStream(Stream, ptr, Apu.MaxSamples);
                }
            }
        }

        public void Unload()
        {
            Raylib.UnloadAudioStream(Stream);
            Raylib.CloseAudioDevice();
        }
    }
}
