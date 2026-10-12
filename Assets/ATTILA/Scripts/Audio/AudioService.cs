using System.Collections.Generic;
using ATTILA.Save;
using UnityEngine;

namespace ATTILA.Audio
{
    /// <summary>
    /// Single entry point for music, ambience and effects. Volumes come from the save file, so the settings
    /// screens and the pause menu only have to call the setters. Created on first use and kept across scenes.
    /// </summary>
    public sealed class AudioService : MonoBehaviour
    {
        private const int SfxVoices = 6;
        private const float MusicFadeSeconds = 1.2f;
        private const float AmbientLevel = .35f;
        private static AudioService instance;
        private readonly Dictionary<Sfx, AudioClip> effects = new();
        private readonly Dictionary<MusicTrack, AudioClip> tracks = new();
        private readonly AudioSource[] voices = new AudioSource[SfxVoices];
        private AudioSource musicSource, fadingSource, ambientSource;
        private MusicTrack currentTrack;
        private float fade = 1f;
        private int nextVoice;

        private static AudioService Instance
        {
            get
            {
                if (instance != null) return instance;
                var go = new GameObject("Audio Service");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<AudioService>();
                instance.Build();
                return instance;
            }
        }

        public static MusicTrack CurrentTrack => instance != null ? instance.currentTrack : MusicTrack.None;

        public static void Play(Sfx sfx, float volume = 1f, float pitch = 1f) => Instance.PlayEffect(sfx, volume, pitch);

        /// <summary>Keeps exactly one listener alive: ours, unless a scene camera brought its own.</summary>
        private void EnsureSingleListener()
        {
            var mine = GetComponent<AudioListener>();
            if (mine == null) return;
            foreach (var other in FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != mine && other.enabled) { mine.enabled = false; return; }
            mine.enabled = true;
        }
        public static void PlayMusic(MusicTrack track) => Instance.SwitchMusic(track);

        public static void SetMusicVolume(float value)
        {
            SaveService.Current.musicVolume = Mathf.Clamp01(value);
            if (instance != null) instance.ApplyMusicVolume();
        }

        public static void SetSfxVolume(float value) => SaveService.Current.sfxVolume = Mathf.Clamp01(value);

        private void Build()
        {
            // The menu scenes have no camera, so nothing could be heard there until the service brought its own ear.
            if (GetComponent<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            for (int i = 0; i < SfxVoices; i++) voices[i] = NewSource("Sfx " + i, false);
            musicSource = NewSource("Music A", true);
            fadingSource = NewSource("Music B", true);
            ambientSource = NewSource("Ambient", true);
            ambientSource.clip = ProceduralAudio.Wind();
        }

        private AudioSource NewSource(string sourceName, bool loop)
        {
            var source = new GameObject(sourceName).AddComponent<AudioSource>();
            source.transform.SetParent(transform, false);
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        private void PlayEffect(Sfx sfx, float volume, float pitch)
        {
            if (!effects.TryGetValue(sfx, out var clip)) effects[sfx] = clip = ProceduralAudio.Effect(sfx);
            var voice = voices[nextVoice++ % SfxVoices];
            voice.pitch = pitch;
            voice.PlayOneShot(clip, Mathf.Clamp01(volume) * SaveService.Current.sfxVolume);
        }

        private void SwitchMusic(MusicTrack track)
        {
            if (track == currentTrack) return;
            currentTrack = track;
            (musicSource, fadingSource) = (fadingSource, musicSource);
            if (track == MusicTrack.None) { musicSource.Stop(); }
            else
            {
                if (!tracks.TryGetValue(track, out var clip)) tracks[track] = clip = ProceduralAudio.Music(track);
                musicSource.clip = clip;
                musicSource.volume = 0f;
                musicSource.Play();
            }
            bool wind = track == MusicTrack.Aul || track == MusicTrack.Night;
            if (wind && !ambientSource.isPlaying) ambientSource.Play();
            else if (!wind) ambientSource.Stop();
            fade = 0f;
        }

        private float nextListenerCheck;

        private void Update()
        {
            if (Time.unscaledTime > nextListenerCheck)
            {
                nextListenerCheck = Time.unscaledTime + .5f;
                EnsureSingleListener();
            }
            if (fade < 1f) fade = Mathf.Min(1f, fade + Time.unscaledDeltaTime / MusicFadeSeconds);
            ApplyMusicVolume();
        }

        private void ApplyMusicVolume()
        {
            float volume = SaveService.Current.musicVolume;
            if (musicSource != null) musicSource.volume = currentTrack == MusicTrack.None ? 0f : volume * fade;
            if (fadingSource != null) fadingSource.volume = volume * (1f - fade);
            if (fadingSource != null && fade >= 1f && fadingSource.isPlaying) fadingSource.Stop();
            if (ambientSource != null) ambientSource.volume = volume * AmbientLevel;
        }
    }
}
