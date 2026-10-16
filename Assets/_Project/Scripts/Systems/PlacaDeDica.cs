using UnityEngine;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Placa atras de um texto de dica no cenario do Tutorial.
    ///
    /// As dicas eram texto branco solto no ceu, sem nada que dissesse "isto e
    /// uma placa". Pareciam anotacao esquecida na cena, e competiam com o fundo
    /// claro da lua. Uma placa escura com um fio dourado no topo (o mesmo
    /// dourado da moldura da tela de tutorial) separa a dica do cenario e da a
    /// ela cara de elemento de jogo.
    ///
    /// O tamanho sai do proprio texto, medido quando a cena comeca: trocar o
    /// texto ou a fonte nao exige ajustar a placa a mao.
    /// </summary>
    [RequireComponent(typeof(TextMesh))]
    public class PlacaDeDica : MonoBehaviour
    {
        [SerializeField] private Color corPlaca = new Color(0.03f, 0.02f, 0.06f, 0.72f);
        [SerializeField] private Color corFio = new Color(0.95f, 0.80f, 0.42f, 0.85f);
        [SerializeField] private Vector2 folga = new Vector2(0.35f, 0.2f);
        [SerializeField] private float espessuraFio = 0.04f;

        private void Start()
        {
            var texto = GetComponent<MeshRenderer>();
            if (texto == null) return;

            Bounds b = texto.bounds;
            Vector2 tamanho = new Vector2(b.size.x + folga.x * 2f, b.size.y + folga.y * 2f);
            int ordem = texto.sortingOrder;

            Retangulo("Placa", b.center, tamanho, corPlaca, ordem - 2);
            Vector3 topo = new Vector3(b.center.x, b.center.y + tamanho.y * 0.5f, b.center.z);
            Retangulo("Fio", topo, new Vector2(tamanho.x, espessuraFio), corFio, ordem - 1);
        }

        private void Retangulo(string nome, Vector3 centro, Vector2 tamanho, Color cor, int ordem)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(transform, false);
            go.transform.position = centro;

            // O sprite solido mede 1 x 1 unidade; a escala vira o tamanho, ja
            // descontada a escala de quem e o pai.
            Vector3 pai = transform.lossyScale;
            go.transform.localScale = new Vector3(tamanho.x / Mathf.Max(0.0001f, pai.x),
                                                  tamanho.y / Mathf.Max(0.0001f, pai.y), 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = RuntimeSprites.Solido();
            sr.color = cor;
            sr.sortingOrder = ordem;
        }
    }
}
