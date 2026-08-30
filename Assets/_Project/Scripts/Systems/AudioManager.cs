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

        [Header("Mixagem")]
        // As duas fontes nasciam no volume padrao 1,0 e nada mudava isso: nenhum
        // ponto do jogo chamava SetMusicVolume ou SetSfxVolume. A trilha tocava
        // no mesmo nivel do golpe, do dano e da coleta, e engolia os efeitos -
        // justamente os sons que informam o jogador do que acabou de acontecer.
        //
        // A musica sustenta o clima e pode ficar bem abaixo sem se perder; o SFX
        // e informacao e fica logo abaixo do teto, com folga para os picos.
        [Tooltip("Volume da trilha de fundo. Fica abaixo do SFX: e clima, nao informacao.")]
        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 0.55f;
        [Tooltip("Volume dos efeitos. Sao eles que dizem ao jogador o que aconteceu.")]
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 0.9f;

        // Ganho da faixa que esta tocando e intensidade momentanea dela. Nao sao
        // ajustes do jogador: sao mixagem, e por isso nao aparecem no Inspector.
        private float trackGain = 1f;
        private float musicIntensity = 1f;

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

            // Aplicado aqui, e nao so quando alguem pedir: sem isto a mixagem
            // depende de um chamador que nunca existiu.
            SetMusicVolume(musicVolume);
            SetSfxVolume(sfxVolume);
        }

        /// <summary>
        /// Toca uma trilha. <paramref name="trackGain"/> compensa a diferenca de
        /// nivel ENTRE as faixas: elas vem de pacotes diferentes e nao foram
        /// masterizadas juntas. O tema do chefe, por exemplo, e gravado quase
        /// quatro vezes mais alto que as trilhas das fases - sem correcao, entrar
        /// na arena seria um susto de volume, e baixar a musica no geral so
        /// deixaria as outras fases inaudiveis.
        /// </summary>
        public void PlayMusic(AudioClip clip, bool loop = true, float trackGain = 1f)
        {
            if (clip == null) return;
            musicSource.loop = loop;

            // A intensidade e propria de cada trilha: quem entra numa cena nova
            // comeca do zero, senao a escalada da luta do chefe vazaria para a
            // tela de Vitoria (este objeto sobrevive a troca de cena).
            musicIntensity = 1f;
            this.trackGain = Mathf.Max(0f, trackGain);

            // Evita reiniciar a mesma trilha que ja esta tocando.
            if (musicSource.clip == clip && musicSource.isPlaying) { AplicarVolumeDaMusica(); return; }

            musicSource.clip = clip;
            AplicarVolumeDaMusica();
            musicSource.Play();
        }

        /// <summary>
        /// Multiplicador de intensidade da trilha atual, para a musica reagir ao
        /// que esta acontecendo - a luta do chefe aperta a cada fase dele. Nao
        /// substitui o volume do jogador nem o ganho da faixa: multiplica os dois.
        /// </summary>
        public void SetMusicIntensity(float intensity)
        {
            musicIntensity = Mathf.Clamp(intensity, 0f, 2f);
            AplicarVolumeDaMusica();
        }

        /// <summary>volume final = escolha do jogador x ganho da faixa x intensidade.</summary>
        private void AplicarVolumeDaMusica()
        {
            if (musicSource != null)
                musicSource.volume = Mathf.Clamp01(musicVolume * trackGain * musicIntensity);
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip == null) return;
            sfxSource.PlayOneShot(clip);
        }

        // Guardam tambem o valor escolhido, para um futuro menu de opcoes poder
        // ler de volta o que esta valendo.
        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            AplicarVolumeDaMusica();
        }

        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            if (sfxSource != null) sfxSource.volume = sfxVolume;
        }
    }
}
