using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using EcosDeAldenor.UI;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Um objetivo que a fase cobra antes de abrir a porta de saida, alem das
    /// almas: os sinos da Cidade, os cirios do Claustro.
    ///
    /// Segue a mesma regra das almas na PhaseExit: o que ainda nao foi cumprido
    /// NA CENA e o que falta, sem registro em lugar nenhum de quantos havia.
    ///
    /// O jogador ativa o objetivo com a espada. Para isso ele tem um
    /// HealthSystem de 1 ponto na camada dos inimigos - o golpe do Ren ja sabe
    /// acertar isso, e com faisca e hit stop de graca, o que diz "acertou".
    ///
    /// LEGIBILIDADE. Feitos com a arte dos pacotes, sino e cirio se confundiam
    /// com a decoracao - nada dizia que era preciso fazer algo com eles. Agora
    /// um objetivo pendente se anuncia de tres jeitos: um aviso ao entrar na
    /// fase dizendo o que fazer, a placa "N / 3" no HUD, e, no mundo, um brilho
    /// dourado pulsando e um losango dourado flutuando acima dele (o dourado das
    /// almas, que o jogador ja aprendeu a procurar). Cumprido, os dois somem.
    /// </summary>
    [RequireComponent(typeof(HealthSystem))]
    public abstract class ObjetivoDaFase : MonoBehaviour
    {
        public bool Cumprido { get; private set; }

        /// <summary>Avisa o HUD sempre que um objetivo da cena e cumprido.</summary>
        public static event Action OnMudou;

        /// <summary>Aviso da porta trancada, ex.: "AINDA HÁ 2 SINOS EM SILÊNCIO".</summary>
        public abstract string Pendente(int quantos);

        /// <summary>Segunda linha do aviso: o que fazer.</summary>
        public abstract string Dica { get; }

        /// <summary>Anuncio quando o ultimo objetivo desta especie e cumprido.</summary>
        public abstract string TodosCumpridos { get; }

        /// <summary>Anuncio ao entrar na fase: o que ha para fazer.</summary>
        public abstract string Introducao { get; }

        /// <summary>Icone da placa do HUD.</summary>
        public abstract Sprite Icone { get; }

        /// <summary>Onde o losango flutua (acima do objeto) e onde o brilho fica.</summary>
        protected abstract Vector3 TopoDoObjeto { get; }
        protected abstract Vector3 CentroDoObjeto { get; }

        protected HealthSystem vida;

        private static readonly Color Ouro = new Color(1f, 0.82f, 0.42f, 1f);
        private SpriteRenderer marcador;
        private SpriteRenderer brilho;
        private float fase;
        private static int ultimaCenaAnunciada = -1;

        protected virtual void Awake()
        {
            vida = GetComponent<HealthSystem>();
            vida.OnDeath += AoSerAtingido;
            fase = UnityEngine.Random.value * 6f;
        }

        protected virtual void Start()
        {
            // Fora de uma fase jogavel (o epilogo usa os sinos da cidade como
            // cenario e os toca ele mesmo), nada de marcador nem de aviso.
            if (FindAnyObjectByType<PhaseExit>() == null) return;

            brilho = Enfeite("Brilho", RuntimeSprites.Brilho(), -3);
            brilho.transform.localScale = new Vector3(2.4f, 2.6f, 1f);
            marcador = Enfeite("Marcador", ArteGerada.Marcador(), 8);

            // Um aviso por fase, dado por quem chegar primeiro.
            int cena = gameObject.scene.handle;
            if (ultimaCenaAnunciada != cena)
            {
                ultimaCenaAnunciada = cena;
                StartCoroutine(Anunciar());
            }
        }

        private IEnumerator Anunciar()
        {
            yield return new WaitForSeconds(1.2f);
            var ui = FindAnyObjectByType<UIManager>();
            if (ui != null) ui.MostrarAviso(Introducao, 3.2f);
        }

        private SpriteRenderer Enfeite(string nome, Sprite sprite, int ordem)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(transform.parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = ordem;
            sr.color = Ouro;
            return sr;
        }

        protected virtual void OnDestroy()
        {
            if (vida != null) vida.OnDeath -= AoSerAtingido;
        }

        protected virtual void Update()
        {
            if (Cumprido || marcador == null) return;

            // Losango sobe e desce devagar; o brilho respira por tras do objeto.
            float t = Time.time + fase;
            marcador.transform.position = TopoDoObjeto + new Vector3(0f, 0.12f * Mathf.Sin(t * 3f), 0f);
            brilho.transform.position = CentroDoObjeto;
            brilho.color = new Color(Ouro.r, Ouro.g, Ouro.b, 0.22f + 0.14f * Mathf.Sin(t * 2.4f));
            if (Mathf.Repeat(t, 1.1f) < Time.deltaTime)
                Efeitos.Brasas(CentroDoObjeto, 1, Ouro, forca: 0.4f, subida: 1.2f, tamanho: 0.1f, vida: 1f);
        }

        private void AoSerAtingido()
        {
            if (Cumprido) return;
            Cumprido = true;

            // O objeto continua na cena (o sino fica pendurado, o cirio fica la
            // apagado): so deixa de ser alvo do golpe e de chamar atencao.
            foreach (var col in GetComponents<Collider2D>()) col.enabled = false;
            if (marcador != null) Destroy(marcador.gameObject);
            if (brilho != null) Destroy(brilho.gameObject);
            AoCumprir();
            OnMudou?.Invoke();

            var mesmos = FindObjectsByType<ObjetivoDaFase>(FindObjectsInactive.Exclude)
                         .Where(o => o.GetType() == GetType()).ToList();
            int feitos = mesmos.Count(o => o.Cumprido);
            var ui = FindAnyObjectByType<UIManager>();
            if (ui == null) return;

            if (feitos >= mesmos.Count) ui.MostrarAviso(TodosCumpridos, 2.6f);
            else ui.MostrarAviso(string.Format("{0} / {1}", feitos, mesmos.Count), 1.2f);
        }

        /// <summary>O que acontece na hora: som, luz, efeito.</summary>
        protected abstract void AoCumprir();

        /// <summary>
        /// Quantos objetivos ainda faltam nesta cena, e o primeiro deles (para
        /// a porta saber que texto mostrar).
        /// </summary>
        public static int Pendentes(out ObjetivoDaFase exemplo)
        {
            var faltam = FindObjectsByType<ObjetivoDaFase>(FindObjectsInactive.Exclude).Where(o => !o.Cumprido).ToList();
            var primeiro = faltam.FirstOrDefault();
            exemplo = primeiro;
            return primeiro == null ? 0 : faltam.Count(o => o.GetType() == primeiro.GetType());
        }
    }
}
