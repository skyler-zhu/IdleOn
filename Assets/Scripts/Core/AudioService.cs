using UnityEngine;

namespace IdleOnLike.Core
{
    public sealed class AudioService
    {
        private readonly AudioSource bgmSource;
        private readonly AudioSource sfxSource;

        public AudioService(Transform parent)
        {
            var audioRoot = new GameObject("Audio Service");
            audioRoot.transform.SetParent(parent, false);

            bgmSource = audioRoot.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = 0.45f;

            sfxSource = audioRoot.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = 0.8f;
        }

        public void PlayBgm(AudioClip clip)
        {
            if (clip == null)
            {
                bgmSource.Stop();
                bgmSource.clip = null;
                return;
            }

            if (bgmSource.clip == clip && bgmSource.isPlaying)
            {
                return;
            }

            bgmSource.clip = clip;
            bgmSource.Play();
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }
    }
}
