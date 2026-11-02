using System.Collections;
using System.Linq;
using UnityEngine;
using EcosDeAldenor.Player;
using EcosDeAldenor.Systems;

namespace EcosDeAldenor.UI
{
    /// <summary>
    /// PROLOGO, PARTE 3: o Ren desperta no cemiterio - no proprio tutorial.
    ///
    /// O prologo nao termina num cartao e corta para a fase: termina DENTRO da
    /// fase. O Ren esta caido entre as lapides, levanta (a animacao de morte
    /// dele ao contrario), seis ecos acendem no ceu e partem cada um para um
    /// lado, e o controle passa ao jogador - com a tela de controles do
    /// tutorial logo em seguida.
    ///
    /// So acontece vindo do prologo (Pendente). Quem pula a historia, ou volta
    /// ao tutorial depois, cai direto na tela de controles.
    /// </summary>
    public class DespertarDoRen : MonoBehaviour
    {
        /// <summary>Marcado pelo fim do prologo; o tutorial seguinte encena o despertar.</summary>
        public static bool Pendente;

        /// <summary>Verdadeiro enquanto a cena roda; a tela de tutorial espera.</summary>
        public static bool EmCena { get; private set; }

        [SerializeField] private Sprite[] renMorrendo;
        [SerializeField] private AudioClip somDoDespertar;
        [SerializeField] private AudioClip somDoEco;

        private void Awake()
        {
            EmCena = Pendente;
            Pendente = false;
        }

        private void Start()
        {
            if (EmCena) StartCoroutine(Cena());
        }

        private void OnDestroy() => EmCena = false;

        private IEnumerator Cena()
        {
            var renGO = GameObject.FindGameObjectWithTag("Player");
            var ren = renGO != null ? renGO.GetComponent<PlayerController>() : null;
            var corpo = renGO != null ? renGO.GetComponent<SpriteRenderer>() : null;
            var pausa = FindAnyObjectByType<PauseManager>();
            if (pausa != null) pausa.enabled = false;
            if (ren != null) ren.ControleBloqueado = true;

            var cinema = Cinema.Criar();
            StartCoroutine(cinema.Faixas(true, 0.01f));

            // O Ren caido: um ator no lugar dele, com o ultimo quadro da morte.
            SpriteRenderer caido = null;
            var levantando = renMorrendo != null ? renMorrendo.Reverse().ToArray() : new Sprite[0];
            if (corpo != null && levantando.Length > 0)
            {
                corpo.enabled = false;
                var go = new GameObject("RenCaido");
                go.transform.position = renGO.transform.position;
                go.transform.localScale = renGO.transform.lossyScale;
                caido = go.AddComponent<SpriteRenderer>();
                caido.sprite = levantando[0];
                caido.flipX = corpo.flipX;
                caido.sortingOrder = corpo.sortingOrder;
                caido.sortingLayerID = corpo.sortingLayerID;
            }
            yield return new WaitForSeconds(0.8f);

            yield return cinema.Narrar("Entre as lápides, o último cavaleiro de pé ainda respirava.", 1.6f);

            // Levanta.
            AudioManager.Instance?.PlaySfx(somDoDespertar);
            if (caido != null)
            {
                foreach (var q in levantando)
                {
                    caido.sprite = q;
                    yield return new WaitForSeconds(1f / 8f);
                }
                Destroy(caido.gameObject);
            }
            if (corpo != null) corpo.enabled = true;
            if (renGO != null) Efeitos.Poeira(renGO.transform.position + new Vector3(0f, -0.85f, 0f), 8, 1.2f);

            // Os seis ecos acendem no ceu e partem.
            var centro = renGO != null ? renGO.transform.position + new Vector3(1.5f, 2.6f, 0f) : new Vector3(0f, 3f, 0f);
            var ecos = new Transform[6];
            for (int i = 0; i < ecos.Length; i++)
            {
                ecos[i] = Encenacao.Alma(centro + new Vector3((i - 2.5f) * 1.1f, Mathf.Sin(i * 1.7f) * 0.6f, 0f),
                                         new Color(1f, 0.75f, 0.9f, 0.95f), 0.45f);
                AudioManager.Instance?.PlaySfx(somDoEco);
                yield return new WaitForSeconds(0.3f);
            }
            yield return cinema.Narrar("Seis ecos ainda brilhavam no escuro. Reúna-os, suba à igreja e encerre a vigília.", 2.4f);
            for (float t = 0f; t < 1.8f; t += Time.deltaTime)
            {
                for (int i = 0; i < ecos.Length; i++)
                    if (ecos[i] != null)
                        ecos[i].position += new Vector3((i - 2.5f) * 2.5f, 1.5f + i * 0.3f, 0f) * Time.deltaTime;
                yield return null;
            }
            foreach (var e in ecos) if (e != null) Destroy(e.gameObject);

            // O controle volta ao jogador, com a tela de controles.
            yield return cinema.Faixas(false, 0.6f);
            Destroy(cinema.gameObject);
            if (ren != null) ren.ControleBloqueado = false;
            if (pausa != null) pausa.enabled = true;
            EmCena = false;
            var tela = FindAnyObjectByType<TutorialScreen>();
            if (tela != null) tela.Abrir();
        }
    }
}
