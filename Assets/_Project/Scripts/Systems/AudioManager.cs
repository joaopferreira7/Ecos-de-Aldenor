using UnityEngine;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Centraliza reproducao de musica e efeitos sonoros. Singleton persistente,
    /// com duas AudioSources separadas (musica e SFX) para permitir controle de
    /// volume independente entre elas.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Fontes de Audio")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
            }
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (clip == null) return;
            musicSource.loop = loop;
            // Evita reiniciar a mesma trilha que ja esta tocando.
            if (musicSource.clip == clip && musicSource.isPlaying) return;
            musicSource.clip = clip;
            musicSource.Play();
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip == null) return;
            sfxSource.PlayOneShot(clip);
        }

        public void SetMusicVolume(float volume) => musicSource.volume = Mathf.Clamp01(volume);
        public void SetSfxVolume(float volume) => sfxSource.volume = Mathf.Clamp01(volume);
    }
}
