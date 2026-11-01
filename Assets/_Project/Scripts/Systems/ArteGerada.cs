using System.Collections.Generic;
using UnityEngine;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// ARTE DOS OBJETOS DAS FASES NOVAS (sino, cirio, braseiro).
    ///
    /// Sempre que o pacote tem a peca, ela sai do pacote: o cirio e o pedestal
    /// com vela do tileset da igreja (a mesma arte das balaustradas da arena),
    /// e o campanario e montado com a madeira do tileset da cidade. Isso e o
    /// Recorte: um pedaco da textura do pacote, com a densidade de pixel dela.
    ///
    /// So o que nenhum pacote traz e desenhado aqui - o sino de bronze e o
    /// braseiro de ferro. A primeira versao desses desenhos tinha pixels quatro
    /// vezes maiores que os do cenario e cores saturadas, e destoava de tudo.
    /// Agora cada desenho e feito NA DENSIDADE do pacote ao lado dele e com a
    /// paleta dele: contorno roxo-escuro, sombras puxadas para o roxo, nada de
    /// branco puro.
    /// </summary>
    public static class ArteGerada
    {
        private static Sprite sino, braseiro, chama, marcador;
        private static readonly Dictionary<string, Sprite> recortes = new Dictionary<string, Sprite>();

        // Paleta dos pacotes GothicVania: contorno e sombra puxados para o roxo.
        private static readonly Color Contorno = new Color(0.14f, 0.09f, 0.15f, 1f);

        /// <summary>
        /// Um pedaco da textura de um pacote, em coordenadas de imagem (y a
        /// partir do TOPO, como num editor de imagem), com o mesmo pixels por
        /// unidade da arte de onde veio.
        /// </summary>
        public static Sprite Recorte(Sprite referencia, int x, int yTopo, int largura, int altura, Vector2 pivo)
        {
            if (referencia == null) return null;
            var tex = referencia.texture;
            string chave = tex.GetInstanceID() + ":" + x + "," + yTopo + "," + largura + "," + altura + "," + pivo;
            if (recortes.TryGetValue(chave, out var pronto) && pronto != null) return pronto;

            var rect = new Rect(x, tex.height - yTopo - altura, largura, altura);
            var s = Sprite.Create(tex, rect, pivo, referencia.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            s.name = "Recorte_" + x + "_" + yTopo;
            s.hideFlags = HideFlags.HideAndDontSave;
            recortes[chave] = s;
            return s;
        }

        /// <summary>
        /// Sino de bronze, na densidade dos props da cidade (100 pixels por
        /// unidade, como eles): o pivo e o alto da alca, onde ele se prende a trave.
        /// </summary>
        public static Sprite Sino()
        {
            if (sino != null) return sino;

            const int L = 44, A = 48;
            var px = Vazio(L, A);
            var escuro = new Color(0.30f, 0.19f, 0.19f, 1f);
            var sombra = new Color(0.43f, 0.28f, 0.24f, 1f);
            var bronze = new Color(0.58f, 0.40f, 0.28f, 1f);
            var luz = new Color(0.74f, 0.55f, 0.36f, 1f);
            var brilho = new Color(0.86f, 0.70f, 0.47f, 1f);
            float meio = (L - 1) * 0.5f;

            // Alca (as linhas sao contadas de baixo para cima).
            for (int y = A - 6; y < A; y++)
                for (int x = 19; x <= 24; x++)
                {
                    bool furo = y >= A - 4 && y <= A - 3 && x >= 21 && x <= 22;
                    bool borda = x == 19 || x == 24 || y == A - 1;
                    if (!furo) Pintar(px, L, x, y, borda ? Contorno : sombra);
                }

            // Corpo: ombro arredondado, cintura reta e a boca abrindo.
            int topo = A - 7, boca = 7;
            for (int y = topo; y >= boca; y--)
            {
                float t = (topo - y) / (float)(topo - boca);
                float meia = t < 0.12f ? 6f + t / 0.12f * 5f       // ombro
                           : t < 0.75f ? 11f + (t - 0.12f) * 9f    // cintura alargando
                           : 16.7f + (t - 0.75f) * 20f;            // boca
                for (int x = 0; x < L; x++)
                {
                    float d = x - meio;
                    if (Mathf.Abs(d) > meia) continue;
                    float lado = d / meia;
                    Color c = Mathf.Abs(d) > meia - 1.2f || y == topo ? Contorno
                            : lado < -0.55f ? brilho
                            : lado < -0.15f ? luz
                            : lado < 0.45f ? bronze
                            : lado < 0.8f ? sombra : escuro;
                    Pintar(px, L, x, y, c);
                }
            }

            // Frisos: um no ombro e dois na boca, como nos sinos de igreja.
            foreach (int y in new[] { topo - 6, boca + 6, boca + 3 })
                for (int x = 0; x < L; x++)
                    if (px[y * L + x].a > 0 && !Igual(px[y * L + x], Contorno))
                        Pintar(px, L, x, y, x < meio - 6 ? bronze : escuro);

            // Labio da boca e o badalo aparecendo embaixo.
            for (int x = 3; x < L - 3; x++) Pintar(px, L, x, boca - 1, Contorno);
            for (int y = 0; y < boca - 1; y++)
                for (int x = 19; x <= 24; x++)
                    Pintar(px, L, x, y, x == 19 || x == 24 || y == 0 ? Contorno : sombra);

            sino = Criar(px, L, A, new Vector2(0.5f, 1f), "ArteSino", 100);
            return sino;
        }

        /// <summary>
        /// Braseiro de ferro sobre tres pes, na densidade do tileset da igreja
        /// (24 pixels por unidade) e com o roxo dele no ferro.
        /// </summary>
        public static Sprite Braseiro()
        {
            if (braseiro != null) return braseiro;

            const int L = 24, A = 22;
            var px = Vazio(L, A);
            var ferro = new Color(0.25f, 0.20f, 0.32f, 1f);
            var ferroLuz = new Color(0.45f, 0.38f, 0.55f, 1f);
            var brasa = new Color(0.86f, 0.36f, 0.16f, 1f);
            var brasaClara = new Color(0.98f, 0.66f, 0.30f, 1f);

            // Pes.
            for (int y = 0; y < 10; y++)
            {
                Pintar(px, L, 4 + y / 3, y, Contorno);
                Pintar(px, L, 5 + y / 3, y, ferro);
                Pintar(px, L, 19 - y / 3, y, Contorno);
                Pintar(px, L, 18 - y / 3, y, ferro);
                Pintar(px, L, 11, y, Contorno);
                Pintar(px, L, 12, y, ferro);
            }
            // Bacia: mais larga em cima, com a borda de cima clara.
            for (int y = 10; y <= 17; y++)
            {
                int meia = Mathf.Min(6 + (y - 10), 11);
                for (int x = 12 - meia; x < 12 + meia; x++)
                {
                    bool borda = x == 12 - meia || x == 12 + meia - 1 || y == 10;
                    Pintar(px, L, x, y, borda ? Contorno : y == 17 ? ferroLuz : (x < 12 - meia + 3 ? ferroLuz : ferro));
                }
            }
            // Carvao.
            for (int x = 2; x < 22; x++)
            {
                Pintar(px, L, x, 18, (x % 3 == 0) ? brasaClara : brasa);
                if (x > 4 && x < 19 && x % 2 == 0) Pintar(px, L, x, 19, brasa);
            }

            braseiro = Criar(px, L, A, new Vector2(0.5f, 0f), "ArteBraseiro", 24);
            return braseiro;
        }

        /// <summary>
        /// Losango que flutua sobre um objetivo pendente - o mesmo motivo dos
        /// losangos dourados das telas do jogo. Desenhado claro, para a cor do
        /// SpriteRenderer pinta-lo; o contorno escuro continua escuro.
        /// </summary>
        public static Sprite Marcador()
        {
            if (marcador != null) return marcador;

            const int L = 13, A = 17;
            var px = Vazio(L, A);
            var claro = new Color(1f, 1f, 1f, 1f);
            var meio = new Color(0.82f, 0.82f, 0.82f, 1f);
            int cx = L / 2;
            // Losango alongado para baixo: aponta para o objeto.
            for (int y = 0; y < A; y++)
            {
                float meia = y < A * 0.6f ? y * (cx / (A * 0.6f)) : (A - 1 - y) * (cx / (A * 0.4f));
                for (int x = 0; x < L; x++)
                {
                    float d = Mathf.Abs(x - cx);
                    if (d > meia + 0.5f) continue;
                    bool borda = d > meia - 0.7f;
                    Pintar(px, L, x, y, borda ? Contorno : (x < cx ? claro : meio));
                }
            }

            marcador = Criar(px, L, A, new Vector2(0.5f, 0f), "ArteMarcador", 24);
            return marcador;
        }

        /// <summary>Uma chama em gota, na densidade do tileset da igreja.</summary>
        public static Sprite Chama()
        {
            if (chama != null) return chama;

            const int L = 9, A = 14;
            var px = Vazio(L, A);
            var vermelho = new Color(0.80f, 0.25f, 0.15f, 1f);
            var laranja = new Color(0.95f, 0.50f, 0.18f, 1f);
            var amarelo = new Color(0.98f, 0.80f, 0.38f, 1f);
            float meio = (L - 1) * 0.5f;
            for (int y = 0; y < A; y++)
            {
                // Gota: larga embaixo, ponta fina em cima.
                float t = y / (float)(A - 1);
                float meia = t < 0.35f ? 2.8f + t * 3f : 3.8f * (1f - (t - 0.35f) / 0.65f);
                for (int x = 0; x < L; x++)
                {
                    float d = Mathf.Abs(x - meio);
                    if (d > meia + 0.2f) continue;
                    float k = d / Mathf.Max(0.5f, meia);
                    Pintar(px, L, x, y, k > 0.75f ? vermelho : (k > 0.35f || t > 0.6f) ? laranja : amarelo);
                }
            }

            chama = Criar(px, L, A, new Vector2(0.5f, 0f), "ArteChama", 24);
            return chama;
        }

        // ------------------------------------------------------------------

        private static Color32[] Vazio(int largura, int altura) => new Color32[largura * altura];

        private static bool Igual(Color32 a, Color b) => a.Equals((Color32)b);

        private static void Pintar(Color32[] px, int largura, int x, int y, Color cor)
        {
            if (x < 0 || y < 0 || x >= largura || y * largura + x >= px.Length) return;
            px[y * largura + x] = cor;
        }

        private static Sprite Criar(Color32[] px, int largura, int altura, Vector2 pivo, string nome, float pixelsPorUnidade)
        {
            var tex = new Texture2D(largura, altura, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.SetPixels32(px);
            tex.Apply();

            var s = Sprite.Create(tex, new Rect(0, 0, largura, altura), pivo, pixelsPorUnidade);
            s.name = nome;
            s.hideFlags = HideFlags.HideAndDontSave;
            return s;
        }
    }
}
