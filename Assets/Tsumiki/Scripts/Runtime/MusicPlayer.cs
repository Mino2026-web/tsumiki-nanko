using UnityEngine;

namespace Tsumiki.Runtime
{
    public sealed class MusicPlayer : MonoBehaviour
    {
        private static readonly string[] TrackPaths =
        {
            "Audio/BGM/mozart_k331_tema",
            "Audio/BGM/mozart_k545_andante",
            "Audio/BGM/mozart_k265_variations"
        };

        private AudioSource source;
        private AudioClip[] tracks;
        private int trackIndex;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePlayer()
        {
            if (!FindAnyObjectByType<MusicPlayer>())
                new GameObject("BGM（モーツァルト）").AddComponent<MusicPlayer>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.volume = .22f;
            tracks = new AudioClip[TrackPaths.Length];
            for (var i = 0; i < TrackPaths.Length; i++) tracks[i] = Resources.Load<AudioClip>(TrackPaths[i]);
        }

        private void Update()
        {
            var enabledByUser = PlayerPrefs.GetInt("bgm", 1) == 1;
            if (!enabledByUser)
            {
                if (source.isPlaying) source.Pause();
                return;
            }

            if (source.clip && !source.isPlaying && source.time > 0f && source.time < source.clip.length - .1f)
            {
                source.UnPause();
                return;
            }

            if (!source.isPlaying) PlayNextTrack();
        }

        private void PlayNextTrack()
        {
            for (var attempt = 0; attempt < tracks.Length; attempt++)
            {
                var clip = tracks[trackIndex];
                trackIndex = (trackIndex + 1) % tracks.Length;
                if (!clip) continue;
                source.clip = clip;
                source.Play();
                return;
            }
        }
    }
}
