using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace MusicVault.Services
{
    public class AudioService : IDisposable
    {
        private WaveOutEvent? output;
        private AudioFileReader? reader;
        private SmbPitchShiftingSampleProvider? pitch;

        private long trimStart = 0;
        private long trimEnd = -1;

        private bool manualStop = false;

        public event Action? PlaybackStopped;

        public bool IsPlaying => output?.PlaybackState == PlaybackState.Playing;

        public long Position =>
            reader != null ? (long)reader.CurrentTime.TotalMilliseconds : 0;

        public long Duration =>
            reader != null ? (long)reader.TotalTime.TotalMilliseconds : 0;

        public float Volume
        {
            get => reader?.Volume ?? 1f;
            set { if (reader != null) reader.Volume = value; }
        }

        public void Play(string path, int pitchSemitones, long start, long end)
        {
            Stop(); // prevent overlap

            manualStop = false;

            reader = new AudioFileReader(path);

            trimStart = start;
            trimEnd = end <= 0
                ? (long)reader.TotalTime.TotalMilliseconds
                : end;

            reader.CurrentTime = TimeSpan.FromMilliseconds(trimStart);

            var sample = reader.ToSampleProvider();

            pitch = new SmbPitchShiftingSampleProvider(sample);
            SetPitch(pitchSemitones);

            output = new WaveOutEvent();
            output.Init(pitch);

            output.PlaybackStopped += (s, e) =>
            {
                if (!manualStop)
                    PlaybackStopped?.Invoke();

                manualStop = false;
            };

            output.Play();
        }

        public void SetPitch(int semitones)
        {
            if (pitch == null) return;

            float factor = (float)Math.Pow(2.0, semitones / 12.0);
            pitch.PitchFactor = factor;
        }

        public void Pause() => output?.Pause();
        public void Resume() => output?.Play();

        public void Seek(long ms)
        {
            if (reader == null) return;

            long pos = Math.Max(trimStart, Math.Min(ms, trimEnd));
            reader.CurrentTime = TimeSpan.FromMilliseconds(pos);
        }

        public void Update()
        {
            if (reader == null || output == null) return;

            if (Position >= trimEnd)
            {
                manualStop = false; // allow auto-next
                output.Stop();
            }
        }

        public void Stop()
        {
            manualStop = true;

            output?.Stop();
            reader?.Dispose();
            output?.Dispose();

            reader = null;
            output = null;
            pitch = null;
        }

        public void Dispose() => Stop();
    }
}