using System.IO;
using System.Media;

namespace BloodborneLauncher.Core
{
    /// <summary>
    /// The trophy sound: a short two-note bell synthesised here (so no sound file has to ship), or
    /// a .wav of your own. Volume scales the samples, since SoundPlayer has no volume of its own.
    /// </summary>
    public static class Chime
    {
        private const int Rate = 44100;

        public static void Play(int volumePercent, string customFile = "")
        {
            try
            {
                if (customFile.Length > 0 && File.Exists(customFile))
                {
                    new SoundPlayer(customFile).Play();
                    return;
                }
                var player = new SoundPlayer(new MemoryStream(Wave(Math.Clamp(volumePercent, 0, 100) / 100.0)));
                player.Play();
            }
            catch
            {
                // No audio device: the notification still shows.
            }
        }

        /// <summary>16-bit mono PCM: a soft low note, then a brighter one, each a bell (fundamental
        /// plus decaying inharmonic partials).</summary>
        private static byte[] Wave(double volume)
        {
            var seconds = 1.6;
            var count = (int)(Rate * seconds);
            var samples = new double[count];
            Bell(samples, 0.00, 659.25, 0.55);  // E5
            Bell(samples, 0.14, 987.77, 0.65);  // B5

            var peak = samples.Max(Math.Abs);
            using var memory = new MemoryStream();
            using var writer = new BinaryWriter(memory);
            writer.Write("RIFF"u8);
            writer.Write(36 + count * 2);
            writer.Write("WAVEfmt "u8);
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(Rate);
            writer.Write(Rate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write("data"u8);
            writer.Write(count * 2);
            var gain = peak > 0 ? 0.8 * volume / peak : 0;
            foreach (var s in samples)
                writer.Write((short)Math.Clamp(s * gain * short.MaxValue, short.MinValue, short.MaxValue));
            writer.Flush();
            return memory.ToArray();
        }

        private static void Bell(double[] samples, double start, double frequency, double amplitude)
        {
            var partials = new (double Ratio, double Level, double Decay)[]
            {
                (1.0, 1.0, 2.6), (2.0, 0.35, 4.0), (2.76, 0.18, 6.0), (5.4, 0.06, 9.0),
            };
            var first = (int)(start * Rate);
            for (var i = first; i < samples.Length; i++)
            {
                var t = (i - first) / (double)Rate;
                var attack = Math.Min(1.0, t / 0.006);
                var value = 0.0;
                foreach (var (ratio, level, decay) in partials)
                    value += level * Math.Exp(-decay * t) * Math.Sin(2 * Math.PI * frequency * ratio * t);
                samples[i] += amplitude * attack * value;
            }
        }
    }
}
