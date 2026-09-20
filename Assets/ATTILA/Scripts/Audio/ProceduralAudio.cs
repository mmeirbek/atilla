using UnityEngine;

namespace ATTILA.Audio
{
    public enum Sfx { Hover, Click, Blip, Reward, Fail, Step, Ring, Toss, Hit }
    public enum MusicTrack { None, Menu, Select, Aul, Night, Battle, Map }

    /// <summary>
    /// Placeholder sound synthesised in code: plucked strings (Karplus-Strong) on a pentatonic scale for the music,
    /// filtered noise for wind and short tones for interface effects. Real recordings can replace it through <see cref="AudioService"/>.
    /// </summary>
    public static class ProceduralAudio
    {
        public const int SampleRate = 44100;
        private const int Rest = int.MinValue;
        private static readonly int[] Pentatonic = { 0, 3, 5, 7, 10 };
        private static readonly int[] PhraseA = { 0, 2, 3, 2, 4, 3, 2, 0, 2, 3, 4, 5, 4, 3, 2, -1 };
        private static readonly int[] PhraseB = { 5, 4, 3, 4, 2, 3, 2, 0, -1, 0, 2, 0, 3, 2, 0, -1 };
        private static readonly int[] PhraseSparse = { 0, Rest, 2, Rest, 3, Rest, 2, Rest, 4, Rest, 3, Rest, 2, Rest, 0, Rest };
        private static readonly int[] PhraseDrive = { 0, 0, 3, 0, 4, 3, 0, -2, 0, 0, 3, 4, 5, 4, 3, 0 };
        private static readonly int[] PhraseTravel = { 0, 2, 4, 5, 7, 5, 4, 2, 0, 2, 4, 2, 3, 2, 0, Rest };

        public static AudioClip Music(MusicTrack track)
        {
            var rng = new System.Random(7 + (int)track);
            switch (track)
            {
                case MusicTrack.Menu: return Compose("Music Menu", rng, 72f, new[] { PhraseSparse, PhraseA, PhraseSparse, PhraseB }, false, .55f);
                case MusicTrack.Select: return Compose("Music Select", rng, 88f, new[] { PhraseB, PhraseA, PhraseB, PhraseSparse }, false, .58f);
                case MusicTrack.Night: return Compose("Music Night", rng, 58f, new[] { PhraseSparse, PhraseSparse, PhraseB, PhraseSparse }, false, .5f);
                case MusicTrack.Battle: return Compose("Music Battle", rng, 132f, new[] { PhraseDrive, PhraseDrive, PhraseB, PhraseDrive }, true, .62f);
                case MusicTrack.Map: return Compose("Music Map", rng, 96f, new[] { PhraseTravel, PhraseA, PhraseTravel, PhraseB }, true, .56f);
                default: return Compose("Music Aul", rng, 104f, new[] { PhraseA, PhraseA, PhraseB, PhraseA }, true, .6f);
            }
        }

        public static AudioClip Wind()
        {
            var rng = new System.Random(3);
            int length = SampleRate * 10, fade = SampleRate;
            var raw = new float[length + fade];
            float low = 0f;
            for (int i = 0; i < raw.Length; i++)
            {
                low += ((float)rng.NextDouble() * 2f - 1f - low) * .02f;
                raw[i] = low;
            }
            var data = new float[length];
            for (int i = 0; i < length; i++)
            {
                float swell = .55f + .45f * Mathf.Sin(2f * Mathf.PI * i / SampleRate / 5f);
                float blend = i < fade ? i / (float)fade : 1f;
                float value = i < fade ? Mathf.Lerp(raw[length + i], raw[i], blend) : raw[i];
                data[i] = value * swell;
            }
            Normalize(data, .5f);
            return ToClip("Wind", data);
        }

        public static AudioClip Effect(Sfx sfx)
        {
            var rng = new System.Random(11 + (int)sfx);
            switch (sfx)
            {
                case Sfx.Hover: return Tone("Hover", .05f, t => Sine(t, 880f), .22f, 40f);
                case Sfx.Click: return Tone("Click", .09f, t => Sine(t, t < .04f ? 660f : 990f) * .8f + Sine(t, 1320f) * .2f, .35f, 30f);
                case Sfx.Blip: return Tone("Blip", .035f, t => Square(t, 540f) * .5f, .18f, 60f);
                case Sfx.Reward: return Arpeggio("Reward", new[] { 293.66f, 349.23f, 440f, 587.33f }, .14f);
                case Sfx.Fail: return Tone("Fail", .42f, t => Triangle(t, Mathf.Lerp(330f, 196f, t / .42f)), .4f, 6f);
                case Sfx.Step: return Noise("Step", .09f, rng, .12f, 55f, .6f);
                case Sfx.Ring: return Bell("Ring");
                case Sfx.Toss: return Noise("Toss", .18f, rng, .5f, 12f, .25f);
                default: return Tone("Hit", .16f, t => Sine(t, 90f) + (float)rng.NextDouble() * .4f - .2f, .5f, 22f);
            }
        }

        private static AudioClip Compose(string name, System.Random rng, float bpm, int[][] phrases, bool gallop, float gain)
        {
            int step = Mathf.RoundToInt(SampleRate * 60f / bpm / 2f);
            int stepsPerPhrase = PhraseA.Length;
            var data = new float[step * stepsPerPhrase * phrases.Length];
            for (int p = 0; p < phrases.Length; p++)
            {
                for (int s = 0; s < stepsPerPhrase; s++)
                {
                    int start = (p * stepsPerPhrase + s) * step;
                    int note = phrases[p][s];
                    if (note != Rest) Pluck(data, start, NoteFrequency(note), .55f, rng);
                    // Second string of the dombra: a quiet drone a fourth below, with a galloping accent when requested.
                    bool droneStep = gallop ? s % 8 == 0 || s % 8 == 3 || s % 8 == 4 || s % 8 == 7 : s % 8 == 0;
                    if (droneStep) Pluck(data, start, s % 8 == 0 ? 146.83f : 196f, gallop ? .3f : .22f, rng);
                }
            }
            Normalize(data, gain);
            return ToClip(name, data);
        }

        private static float NoteFrequency(int pentatonicIndex)
        {
            int octave = Mathf.FloorToInt(pentatonicIndex / 5f);
            int degree = pentatonicIndex - octave * 5;
            return 293.66f * Mathf.Pow(2f, (Pentatonic[degree] + 12 * octave) / 12f);
        }

        // Karplus-Strong: a noise burst circulating through a short averaging delay line rings like a plucked string.
        private static void Pluck(float[] buffer, int start, float frequency, float amplitude, System.Random rng)
        {
            int period = Mathf.Max(2, Mathf.RoundToInt(SampleRate / frequency));
            var line = new float[period];
            for (int i = 0; i < period; i++) line[i] = (float)rng.NextDouble() * 2f - 1f;
            int length = Mathf.Min(buffer.Length, Mathf.RoundToInt(SampleRate * 1.6f));
            int index = 0;
            for (int i = 0; i < length; i++)
            {
                int next = (index + 1) % period;
                float value = line[index];
                line[index] = (value + line[next]) * .5f * .9968f;
                index = next;
                buffer[(start + i) % buffer.Length] += value * amplitude * Mathf.Min(1f, i / 60f);
            }
        }

        private static AudioClip Tone(string name, float seconds, System.Func<float, float> wave, float volume, float decay)
        {
            var data = new float[Mathf.RoundToInt(SampleRate * seconds)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)SampleRate;
                data[i] = wave(t) * volume * Mathf.Exp(-decay * t) * Mathf.Min(1f, i / 80f);
            }
            return ToClip(name, data);
        }

        private static AudioClip Noise(string name, float seconds, System.Random rng, float volume, float decay, float smoothing)
        {
            var data = new float[Mathf.RoundToInt(SampleRate * seconds)];
            float low = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                low += ((float)rng.NextDouble() * 2f - 1f - low) * smoothing;
                data[i] = low * volume * Mathf.Exp(-decay * i / SampleRate);
            }
            return ToClip(name, data);
        }

        private static AudioClip Arpeggio(string name, float[] notes, float spacing)
        {
            var rng = new System.Random(5);
            var data = new float[Mathf.RoundToInt(SampleRate * (spacing * notes.Length + 1f))];
            for (int i = 0; i < notes.Length; i++) Pluck(data, Mathf.RoundToInt(SampleRate * spacing * i), notes[i], .5f, rng);
            Normalize(data, .55f);
            return ToClip(name, data);
        }

        private static AudioClip Bell(string name)
        {
            float[] ratios = { 1f, 2.76f, 5.4f, 8.93f };
            float[] levels = { 1f, .5f, .28f, .14f };
            return Tone(name, .7f, t =>
            {
                float sum = 0f;
                for (int i = 0; i < ratios.Length; i++) sum += Sine(t, 1180f * ratios[i]) * levels[i] * Mathf.Exp(-t * (4f + i * 3f));
                return sum;
            }, .3f, 0f);
        }

        private static float Sine(float t, float frequency) => Mathf.Sin(2f * Mathf.PI * frequency * t);
        private static float Square(float t, float frequency) => Sine(t, frequency) >= 0f ? 1f : -1f;
        private static float Triangle(float t, float frequency) => 2f * Mathf.Abs(2f * (t * frequency - Mathf.Floor(t * frequency + .5f))) - 1f;

        private static void Normalize(float[] data, float peak)
        {
            float max = .0001f;
            foreach (var sample in data) max = Mathf.Max(max, Mathf.Abs(sample));
            float scale = peak / max;
            for (int i = 0; i < data.Length; i++) data[i] *= scale;
        }

        private static AudioClip ToClip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
