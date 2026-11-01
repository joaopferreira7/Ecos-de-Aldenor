using UnityEngine;
using UnityEngine.Rendering.Universal;
using EcosDeAldenor.Enemies;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// UM DOS TRES SINOS da Cidade Silenciosa (Fase 3).
    ///
    /// O prologo conta que "os sinos se calaram" na noite da nevoa. Aqui o Ren
    /// os faz tocar de novo com a espada - e so com os tres tocando o portao da
    /// colina abre.
    ///
    /// O toque e tambem uma arma: a onda do sino atordoa os inimigos por perto,
    /// o que transforma o sino num ponto a disputar e nao so num item a pegar.
    ///
    /// O sino pende de um pequeno CAMPANARIO montado com as pecas de madeira do
    /// tileset da cidade (dois modulos com mao-francesa, a viga entre eles, os
    /// postes e a sapata de pedra), na mesma escala dos props da cidade. O
    /// objeto fica no ponto onde o sino se prende a viga; so o sino balanca.
    /// </summary>
    public class Sino : ObjetivoDaFase
    {
        [SerializeField] private float raioDaOnda = 4f;
        [SerializeField] private float atordoamento = 1.6f;
        [SerializeField] private LayerMask camadaDosInimigos;
        [Tooltip("Chao onde os postes do campanario se apoiam.")]
        [SerializeField] private LayerMask camadaDoChao;
        [Tooltip("Qualquer fatia do tileset da cidade: dele saem as pecas do campanario.")]
        [SerializeField] private Sprite madeiraDaCidade;
        [Tooltip("A mesma escala dos props da cidade (100 pixels por unidade).")]
        [SerializeField] private float escala = 1.8f;

        // Pecas do tileset da cidade (x, y a partir do topo, largura, altura).
        private static readonly RectInt Modulo = new RectInt(272, 80, 32, 32);
        private static readonly RectInt Viga = new RectInt(320, 80, 16, 13);
        private static readonly RectInt Poste = new RectInt(272, 128, 32, 16);
        private static readonly RectInt Sapata = new RectInt(272, 160, 32, 16);
        private const int VigasNoMeio = 3;

        private Transform corpo;
        private SpriteRenderer desenho;
        private Light2D luz;
        private float balanco;       // amplitude atual do balanco, em graus
        private float relogio;

        public override string Pendente(int quantos) =>
            quantos == 1 ? "AINDA HÁ UM SINO EM SILÊNCIO" : "AINDA HÁ " + quantos + " SINOS EM SILÊNCIO";

        public override string Dica => "<size=20><color=#B9A7D6>Golpeie os sinos para que voltem a tocar</color></size>";

        public override string TodosCumpridos =>
            "Os sinos voltaram a tocar\n<size=26><color=#B9A7D6>O portão da colina se abriu</color></size>";

        public override string Introducao =>
            "Os sinos de Aldenor estão em silêncio\n<size=26><color=#B9A7D6>Golpeie os três sinos para abrir o portão</color></size>";

        public override Sprite Icone => ArteGerada.Sino();
        protected override Vector3 TopoDoObjeto => transform.position + new Vector3(0f, 0.55f, 0f);
        protected override Vector3 CentroDoObjeto => transform.position + new Vector3(0f, -0.45f, 0f);

        protected override void Awake()
        {
            base.Awake();

            var go = new GameObject("Sino");
            corpo = go.transform;
            corpo.SetParent(transform, false);
            corpo.localScale = new Vector3(escala, escala, 1f);
            desenho = go.AddComponent<SpriteRenderer>();
            desenho.sprite = ArteGerada.Sino();
            // Atras do Ren: ele para embaixo do sino para golpea-lo.
            desenho.sortingOrder = -1;
            // Apagado e frio enquanto em silencio.
            desenho.color = new Color(0.62f, 0.6f, 0.72f, 1f);

            luz = gameObject.AddComponent<Light2D>();
            luz.lightType = Light2D.LightType.Point;
            luz.pointLightOuterRadius = 2.2f;
            luz.pointLightInnerRadius = 0.2f;
            luz.color = new Color(1f, 0.8f, 0.45f, 1f);
            luz.intensity = 0f;

            MontarCampanario();
        }

        /// <summary>
        /// Viga no alto, um modulo de cada lado e, debaixo de cada modulo, o
        /// poste ate o chao com a sapata. A altura do poste depende de onde o
        /// sino foi pendurado (chao ou telhado), entao e medida com um raio.
        /// </summary>
        private void MontarCampanario()
        {
            if (madeiraDaCidade == null) return;

            float px = escala / madeiraDaCidade.pixelsPerUnit;     // unidades de mundo por pixel
            var origem = transform.position;
            float topo = origem.y + Viga.height * px;
            float desvio = (VigasNoMeio * Viga.width * 0.5f + Modulo.width * 0.5f) * px;

            for (int i = 0; i < VigasNoMeio; i++)
            {
                float x = origem.x + (i - (VigasNoMeio - 1) * 0.5f) * Viga.width * px;
                Peca("Viga", Viga, new Vector2(0.5f, 1f), new Vector3(x, topo, 0f), false);
            }

            foreach (float lado in new[] { -1f, 1f })
            {
                float x = origem.x + lado * desvio;
                bool espelhar = lado > 0f;
                Peca("Modulo", Modulo, new Vector2(0.5f, 1f), new Vector3(x, topo, 0f), espelhar);

                float baseDoModulo = topo - Modulo.height * px;
                var chao = Physics2D.Raycast(new Vector2(x, baseDoModulo), Vector2.down, 8f, camadaDoChao);
                if (chao.collider == null) continue;

                Peca("Sapata", Sapata, new Vector2(0.5f, 0f), new Vector3(x, chao.point.y, 0f), espelhar);
                float alturaDoPoste = baseDoModulo - (chao.point.y + Sapata.height * px);
                if (alturaDoPoste <= 0.01f) continue;

                var poste = Peca("Poste", Poste, new Vector2(0.5f, 0f),
                                 new Vector3(x, chao.point.y + Sapata.height * px, 0f), espelhar);
                poste.drawMode = SpriteDrawMode.Tiled;
                poste.size = new Vector2(Poste.width / madeiraDaCidade.pixelsPerUnit, alturaDoPoste / escala);
            }
        }

        private SpriteRenderer Peca(string nome, RectInt r, Vector2 pivo, Vector3 posicao, bool espelhar)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(transform, true);
            go.transform.position = posicao;
            go.transform.localScale = new Vector3(escala, escala, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ArteGerada.Recorte(madeiraDaCidade, r.x, r.y, r.width, r.height, pivo);
            sr.flipX = espelhar;
            sr.sortingOrder = -2;
            sr.color = new Color(0.82f, 0.8f, 0.92f, 1f);   // a mesma noite dos props
            return sr;
        }

        protected override void Update()
        {
            base.Update();
            if (balanco <= 0.05f) return;

            // Pendulo amortecido em volta do ponto onde o sino esta preso.
            relogio += Time.deltaTime;
            balanco *= Mathf.Exp(-Time.deltaTime * 0.9f);
            corpo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(relogio * 7f) * balanco);
            if (luz != null) luz.intensity = Mathf.Lerp(luz.intensity, 0.9f, Time.deltaTime * 2f);
        }

        protected override void AoCumprir()
        {
            balanco = 22f;
            relogio = 0f;
            desenho.color = Color.white;
            AudioManager.Instance?.PlaySfx(RuntimeAudio.Sino());

            var centro = transform.position + new Vector3(0f, -0.45f, 0f);
            Efeitos.Onda(centro, raioDaOnda, new Color(1f, 0.85f, 0.5f, 0.9f), 0.7f);
            Efeitos.Lampejo(centro, 3f, new Color(1f, 0.9f, 0.6f, 0.7f));
            CameraShake.Shake(0.12f, 0.08f);

            foreach (var hit in Physics2D.OverlapCircleAll(centro, raioDaOnda, camadaDosInimigos))
            {
                var inimigo = hit.GetComponent<EnemyBase>();
                if (inimigo != null) inimigo.Atordoar(atordoamento);
            }
        }
    }
}
