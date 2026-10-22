using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using EcosDeAldenor.Player;
using EcosDeAldenor.Systems;
using EcosDeAldenor.UI;

namespace EcosDeAldenor.Enemies
{
    /// <summary>
    /// A ENTRADA DA VIGILIA.
    ///
    /// A luta final comecava sem cerimonia: a barra de vida do chefe ja estava
    /// cheia no topo da tela antes de ele aparecer, a trilha de batalha tocava
    /// desde o primeiro quadro da arena, e o chefe simplesmente estava la. Um
    /// chefe final sem apresentacao e o momento em que um jogo de faculdade
    /// mais parece um teste de mecanica.
    ///
    /// Agora a arena comeca em silencio. Quando o Ren cruza este gatilho:
    ///   1. o controle e o chefe param - ninguem ataca durante a cena;
    ///   2. a camera desliza ate a Vigilia;
    ///   3. um toque de encontro, um tremor, e o nome dela entra na tela com o
    ///      titulo; a trilha do chefe comeca neste instante;
    ///   4. a barra de vida surge, a camera volta ao Ren e a luta comeca.
    ///
    /// Sao ~4 s, e so na primeira vez por carregamento da cena. Qualquer tecla
    /// de pulo ou ataque encurta a cena - quem morreu e voltou nao precisa
    /// assistir de novo inteira.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class IntroDoChefe : MonoBehaviour
    {
        [SerializeField] private AVigilia chefe;
        [SerializeField] private BossHealthBar barra;
        [SerializeField] private SceneMusicPlayer trilha;
        [SerializeField] private AudioClip somDeEncontro;

        [Header("Cartao de titulo")]
        [SerializeField] private string nome = "A Vigília";
        [SerializeField] private string epiteto = "Guardiã da Igreja de Aldenor";

        [Header("Tempo")]
        [SerializeField] private float tempoDaCamera = 1.1f;
        [SerializeField] private float tempoDoCartao = 2.2f;

        private bool tocada;
        private bool pular;
        private Coroutine aparecendo;

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D outro)
        {
            if (tocada || !outro.CompareTag("Player")) return;
            tocada = true;
            StartCoroutine(Cena(outro.GetComponent<PlayerController>()));
        }

        private void Update()
        {
            if (!tocada || pular) return;
            if (Input.GetButtonDown("Jump") || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return))
                pular = true;
        }

        /// <summary>Espera o tempo, ou ate o jogador pedir para pular.</summary>
        private IEnumerator Esperar(float segundos)
        {
            float t = 0f;
            while (t < segundos && !pular)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator Cena(PlayerController jogador)
        {
            if (jogador != null)
            {
                jogador.ControleBloqueado = true;
                var rb = jogador.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            if (chefe != null) chefe.enabled = false;

            // A camera e o seguidor do pacote da masmorra: basta trocar o alvo.
            var seguidor = Camera.main != null ? Camera.main.GetComponent<SceneScript.CameraFollow2D>() : null;
            Transform alvoOriginal = seguidor != null ? seguidor.target : null;
            float suavidade = seguidor != null ? seguidor.smoothSpeed : 0f;
            if (seguidor != null && chefe != null)
            {
                seguidor.smoothSpeed = 2.2f;
                seguidor.target = chefe.transform;
            }

            yield return Esperar(tempoDaCamera);

            AudioManager.Instance?.PlaySfx(somDeEncontro);
            CameraShake.Shake(0.35f, 0.25f);
            if (trilha != null) trilha.Tocar();
            var cartao = Cartao();

            yield return Esperar(tempoDoCartao);

            if (barra != null) barra.Revelar();
            if (seguidor != null)
            {
                seguidor.target = alvoOriginal;
                seguidor.smoothSpeed = suavidade;
            }

            // Some o titulo enquanto a camera volta.
            yield return Sumir(cartao, 0.5f);

            if (jogador != null) jogador.ControleBloqueado = false;
            if (chefe != null) chefe.enabled = true;
        }

        private CanvasGroup Cartao()
        {
            // O mesmo cartao serve a transformacao da Vigilia (UIFactory).
            var grupo = UIFactory.CartaoDeTitulo(nome, epiteto, new Color(0.95f, 0.82f, 1f));
            aparecendo = StartCoroutine(Aparecer(grupo, 0.45f));
            return grupo;
        }

        private IEnumerator Aparecer(CanvasGroup g, float duracao)
        {
            for (float t = 0f; t < duracao; t += Time.deltaTime)
            {
                if (g == null) yield break;
                g.alpha = t / duracao;
                yield return null;
            }
            if (g != null) g.alpha = 1f;
        }

        private IEnumerator Sumir(CanvasGroup g, float duracao)
        {
            if (g == null) yield break;
            if (aparecendo != null) StopCoroutine(aparecendo);
            float inicio = g.alpha;
            for (float t = 0f; t < duracao; t += Time.deltaTime)
            {
                g.alpha = Mathf.Lerp(inicio, 0f, t / duracao);
                yield return null;
            }
            Destroy(g.gameObject);
        }
    }
}
