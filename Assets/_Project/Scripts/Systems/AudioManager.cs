using UnityEngine;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Centraliza reproducao de musica e efeitos sonoros. Singleton persistente.
    ///
    /// A musica usa DUAS fontes, para nunca cortar seco:
    ///   - na troca de cena a trilha nova entra enquanto a antiga sai (crossfade);
    ///   - nas faixas com janela de loop (TrilhasDoJogo), o fim emenda no comeco
    ///     com crossfade, em vez de tocar o fade e o silencio do arquivo.
    /// Os efeitos sonoros usam uma terceira fonte, independente.
    ///
    /// Tudo em tempo real (unscaled): a pausa e o hit stop nao seguram a musica.
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

        [Header("Transicoes")]
        [Tooltip("Crossfade entre a trilha de uma cena e a da seguinte.")]
        [SerializeField] private float tempoDeTroca = 1.2f;
        [Tooltip("Crossfade na emenda do loop das faixas com janela de loop.")]
        [SerializeField] private float tempoDeEmenda = 1.5f;

        private const string ChaveMusica = "EcosDeAldenor.VolumeMusica";
        private const string ChaveEfeitos = "EcosDeAldenor.VolumeEfeitos";

        // Ganho da faixa que esta tocando e intensidade momentanea dela. Nao sao
        // ajustes do jogador: sao mixagem, e por isso nao aparecem no Inspector.
        private float trackGain = 1f;
        private float musicIntensity = 1f;

        // As duas fontes de musica, a que esta valendo e o quanto cada uma esta
        // "aberta" (0 a 1) no crossfade.
        private readonly AudioSource[] fontes = new AudioSource[2];
        private readonly float[] abertura = new float[2];
        private readonly float[] alvo = new float[2];
        private readonly float[] velocidade = new float[2];
        private int ativa;
        private TrilhasDoJogo.Trilha janela;
        private bool temJanela;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
            fontes[0] = musicSource;
            fontes[1] = gameObject.AddComponent<AudioSource>();
            foreach (var f in fontes) { f.playOnAwake = false; f.loop = true; f.volume = 0f; }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
            }

            // A escolha do jogador (tela de Opcoes) vale mais que o padrao do
            // Inspector, e sobrevive a fechar o jogo.
            musicVolume = PlayerPrefs.GetFloat(ChaveMusica, musicVolume);
            sfxVolume = PlayerPrefs.GetFloat(ChaveEfeitos, sfxVolume);

            // Aplicado aqui, e nao so quando alguem pedir: sem isto a mixagem
            // depende de um chamador que nunca existiu.
            SetMusicVolume(musicVolume);
            SetSfxVolume(sfxVolume);
        }

        /// <summary>
        /// Toca uma trilha. <paramref name="trackGain"/> compensa a diferenca de
        /// nivel ENTRE as faixas: elas vem de pacotes diferentes e nao foram
        /// masterizadas juntas. Se a faixa esta em TrilhasDoJogo, o ganho de la
        /// vale mais que o da cena.
        /// </summary>
        public void PlayMusic(AudioClip clip, bool loop = true, float trackGain = 1f)
        {
            if (clip == null) return;

            // A intensidade e propria de cada trilha: quem entra numa cena nova
            // comeca do zero, senao a escalada da luta do chefe vazaria para a
            // tela de Vitoria (este objeto sobrevive a troca de cena).
            musicIntensity = 1f;
            temJanela = TrilhasDoJogo.Para(clip.name, out janela);
            this.trackGain = Mathf.Max(0f, temJanela ? janela.ganho : trackGain);
            temJanela = temJanela && loop && janela.fimDoLoop > 0f;

            // Evita reiniciar a mesma trilha que ja esta tocando.
            var atual = fontes[ativa];
            if (atual.clip == clip && atual.isPlaying && alvo[ativa] > 0f) return;

            // A que estava tocando sai; a nova entra na outra fonte. Se nao havia
            // musica, a nova so sobe depressa.
            bool haviaMusica = atual.isPlaying && abertura[ativa] > 0.01f;
            Abrir(ativa, 0f, tempoDeTroca);
            ativa = 1 - ativa;
            var nova = fontes[ativa];
            nova.Stop();
            nova.clip = clip;
            nova.loop = loop;
            nova.time = 0f;
            abertura[ativa] = 0f;
            nova.Play();
            Abrir(ativa, 1f, haviaMusica ? tempoDeTroca : 0.4f);
        }

        /// <summary>
        /// Multiplicador de intensidade da trilha atual, para a musica reagir ao
        /// que esta acontecendo - a luta do chefe aperta a cada fase dele. Nao
        /// substitui o volume do jogador nem o ganho da faixa: multiplica os dois.
        /// </summary>
        public void SetMusicIntensity(float intensity)
        {
            musicIntensity = Mathf.Clamp(intensity, 0f, 2f);
        }

        /// <summary>
        /// Silencia a trilha, com um fade curto. Usado na arena do chefe (a luta
        /// comeca em silencio e a trilha so entra quando ele se revela) e nas
        /// cenas que pedem silencio.
        /// </summary>
        public void StopMusic()
        {
            Abrir(0, 0f, 0.6f);
            Abrir(1, 0f, 0.6f);
        }

        private void Abrir(int i, float para, float duracao)
        {
            alvo[i] = para;
            velocidade[i] = 1f / Mathf.Max(0.01f, duracao);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // A emenda do loop: perto do fim da janela, a outra fonte comeca do
            // inicio da janela e as duas se cruzam.
            var atual = fontes[ativa];
            if (temJanela && atual.isPlaying && alvo[ativa] > 0f && atual.time >= janela.fimDoLoop - tempoDeEmenda)
            {
                Abrir(ativa, 0f, tempoDeEmenda);
                ativa = 1 - ativa;
                var nova = fontes[ativa];
                nova.Stop();
                nova.clip = atual.clip;
                nova.loop = true;
                nova.time = Mathf.Clamp(janela.inicioDoLoop, 0f, atual.clip.length - 0.1f);
                abertura[ativa] = 0f;
                nova.Play();
                Abrir(ativa, 1f, tempoDeEmenda);
            }

            float baseDoVolume = Mathf.Clamp01(musicVolume * trackGain * musicIntensity);
            for (int i = 0; i < 2; i++)
            {
                abertura[i] = Mathf.MoveTowards(abertura[i], alvo[i], velocidade[i] * dt);
                // Curva de potencia constante: no meio do crossfade a soma nao afunda.
                fontes[i].volume = baseDoVolume * Mathf.Sin(abertura[i] * Mathf.PI * 0.5f);
                if (abertura[i] <= 0f && alvo[i] <= 0f && fontes[i].isPlaying)
                {
                    fontes[i].Stop();
                    if (i != ativa) fontes[i].clip = null;
                }
            }
        }

        /// <summary>Grava os volumes atuais como escolha do jogador.</summary>
        public void SalvarVolumes()
        {
            PlayerPrefs.SetFloat(ChaveMusica, musicVolume);
            PlayerPrefs.SetFloat(ChaveEfeitos, sfxVolume);
            PlayerPrefs.Save();
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
        }

        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            if (sfxSource != null) sfxSource.volume = sfxVolume;
        }
    }
}
