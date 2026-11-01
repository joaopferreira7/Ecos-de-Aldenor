using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// UM DOS TRES CIRIOS DO MANTO no Claustro (Fase 4).
    ///
    /// Velas acesas sobre pedestais de pedra na subida para o altar - os selos
    /// da Vigilia: com as tres apagadas, a porta do santuario abre. Ligam a fase
    /// a luta final, em que o manto cai.
    ///
    /// A arte e do tileset da igreja: o pedestal e a coluna da balaustrada (a
    /// mesma das balaustradas da arena) e a vela e a que ja vem acesa em cima
    /// dela. O Ren a apaga com a espada; a vela passa ao recorte sem a chama.
    /// </summary>
    public class Cirio : ObjetivoDaFase
    {
        [Tooltip("Qualquer fatia do tileset da igreja: dele saem o pedestal e a vela.")]
        [SerializeField] private Sprite pedraDaIgreja;
        [Tooltip("1 = a escala das balaustradas da arena.")]
        [SerializeField] private float escala = 0.9f;
        [SerializeField] private AudioClip apagarSfx;

        // Pecas do tileset da igreja (x, y a partir do topo, largura, altura).
        private static readonly RectInt Sapata = new RectInt(257, 128, 15, 16);
        private static readonly RectInt Fuste = new RectInt(257, 123, 15, 5);
        private static readonly RectInt Capitel = new RectInt(257, 103, 15, 15);
        private static readonly RectInt VelaAcesa = new RectInt(261, 92, 7, 11);
        private static readonly RectInt VelaApagada = new RectInt(261, 97, 7, 6);

        private SpriteRenderer vela;
        private Light2D luz;
        private float fase;
        private bool apagado;

        public override string Pendente(int quantos) =>
            quantos == 1 ? "AINDA HÁ UM CÍRIO DO MANTO ACESO" : "AINDA HÁ " + quantos + " CÍRIOS DO MANTO ACESOS";

        public override string Dica => "<size=20><color=#B9A7D6>Apague-os com a espada</color></size>";

        public override string TodosCumpridos =>
            "Os círios se apagaram\n<size=26><color=#B9A7D6>O santuário se abriu</color></size>";

        public override string Introducao =>
            "Os círios do manto ainda queimam\n<size=26><color=#B9A7D6>Apague os três com a espada para abrir o santuário</color></size>";

        public override Sprite Icone => pedraDaIgreja == null ? null
            : ArteGerada.Recorte(pedraDaIgreja, VelaAcesa.x, VelaAcesa.y, VelaAcesa.width, VelaAcesa.height, new Vector2(0.5f, 0f));

        // Altura total do pedestal com a vela: 52 px do tileset.
        private float Altura => pedraDaIgreja == null ? 2f : 52f * escala / pedraDaIgreja.pixelsPerUnit;
        protected override Vector3 TopoDoObjeto => transform.position + new Vector3(0f, Altura + 0.2f, 0f);
        protected override Vector3 CentroDoObjeto => transform.position + new Vector3(0f, Altura * 0.55f, 0f);

        protected override void Awake()
        {
            base.Awake();
            fase = Random.value * 10f;
            if (pedraDaIgreja == null) return;

            // Empilha de baixo para cima: sapata, dois trechos de fuste, capitel, vela.
            int altura = 0;
            altura = Empilhar("Sapata", Sapata, altura);
            altura = Empilhar("Fuste", Fuste, altura);
            altura = Empilhar("Fuste", Fuste, altura);
            altura = Empilhar("Capitel", Capitel, altura);
            vela = Peca("Vela", VelaAcesa, altura);
            vela.color = Color.white;

            float px = escala / pedraDaIgreja.pixelsPerUnit;
            var chama = new GameObject("Luz");
            chama.transform.SetParent(transform, false);
            chama.transform.localPosition = new Vector3(0f, (altura + 8) * px, 0f);
            luz = chama.AddComponent<Light2D>();
            luz.lightType = Light2D.LightType.Point;
            luz.pointLightOuterRadius = 2.4f;
            luz.pointLightInnerRadius = 0.1f;
            luz.color = new Color(1f, 0.55f, 0.35f, 1f);
            luz.intensity = 0.9f;
        }

        private int Empilhar(string nome, RectInt r, int altura)
        {
            Peca(nome, r, altura);
            return altura + r.height;
        }

        private SpriteRenderer Peca(string nome, RectInt r, int alturaEmPixels)
        {
            float px = escala / pedraDaIgreja.pixelsPerUnit;
            var go = new GameObject(nome);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, alturaEmPixels * px, 0f);
            go.transform.localScale = new Vector3(escala, escala, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ArteGerada.Recorte(pedraDaIgreja, r.x, r.y, r.width, r.height, new Vector2(0.5f, 0f));
            sr.sortingOrder = -1;                                   // atras do Ren, como o sino
            sr.color = new Color(0.82f, 0.78f, 0.92f, 1f);          // a pedra da nave, na luz da fase
            return sr;
        }

        protected override void Update()
        {
            base.Update();
            if (apagado || luz == null) return;
            float t = Time.time * 9f + fase;
            luz.intensity = 0.8f + 0.12f * Mathf.Sin(t * 0.7f) + 0.06f * Mathf.Sin(t * 2.3f);
        }

        protected override void AoCumprir()
        {
            apagado = true;
            AudioManager.Instance?.PlaySfx(apagarSfx);
            var pavio = luz != null ? luz.transform.position : transform.position;
            if (luz != null) luz.enabled = false;

            if (vela != null)
                vela.sprite = ArteGerada.Recorte(pedraDaIgreja, VelaApagada.x, VelaApagada.y,
                                                 VelaApagada.width, VelaApagada.height, new Vector2(0.5f, 0f));

            // Fumaca subindo do pavio e um ultimo lampejo avermelhado.
            Efeitos.Brasas(pavio, 8, new Color(0.6f, 0.55f, 0.65f, 1f), forca: 0.5f, subida: 1.4f, tamanho: 0.12f, vida: 1.2f);
            Efeitos.Lampejo(pavio, 1.4f, new Color(1f, 0.4f, 0.3f, 0.6f));
        }
    }
}
