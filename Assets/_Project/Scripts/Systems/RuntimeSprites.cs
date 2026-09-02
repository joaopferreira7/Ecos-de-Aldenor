using UnityEngine;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Fabrica de sprites simples gerados em codigo (quadrado solido, disco e
    /// anel), criados uma unica vez e reaproveitados por todos os chamadores.
    ///
    /// Existe para que a UI construida em runtime e os indicadores de ataque do
    /// chefe nao dependam de arte nova: sao formas geometricas, e desenha-las
    /// em memoria custa alguns kilobytes e nenhum arquivo a mais no projeto.
    /// O jogo ja fazia isso pontualmente (a barra do chefe gera o proprio
    /// quadrado branco); aqui a ideia so foi reunida num lugar reutilizavel.
    /// </summary>
    public static class RuntimeSprites
    {
        private static Sprite solido;
        private static Sprite disco;
        private static Sprite anel;
        private static Sprite brilho;

        /// <summary>Quadrado branco opaco. Serve de base para paineis e barras.</summary>
        public static Sprite Solido()
        {
            if (solido != null) return solido;

            var tex = NovaTextura(8);
            var px = new Color32[8 * 8];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();

            solido = Criar(tex, "RuntimeSolido");
            return solido;
        }

        /// <summary>Disco branco com borda suave - usado como area de impacto no chao.</summary>
        public static Sprite Disco()
        {
            if (disco != null) return disco;
            disco = CriarCircular(0f, "RuntimeDisco", out _);
            return disco;
        }

        /// <summary>
        /// Brilho redondo com queda suave do centro para a borda. Serve de
        /// aura: um disco de borda dura em volta do chefe parece uma bola
        /// solida e come o cenario; o degrade se le como luz.
        /// </summary>
        public static Sprite Brilho()
        {
            if (brilho != null) return brilho;

            const int tam = 128;
            const float meio = tam / 2f;
            var tex = NovaTextura(tam);
            var px = new Color32[tam * tam];

            for (int y = 0; y < tam; y++)
            {
                for (int x = 0; x < tam; x++)
                {
                    float dx = (x + 0.5f - meio) / meio;
                    float dy = (y + 0.5f - meio) / meio;
                    float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    // Queda ao quadrado: nucleo denso e borda que some no fundo.
                    float a = (1f - d) * (1f - d);
                    px[y * tam + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            brilho = Criar(tex, "RuntimeBrilho");
            return brilho;
        }

        /// <summary>Anel branco (so a borda) - contorno da area de impacto.</summary>
        public static Sprite Anel()
        {
            if (anel != null) return anel;
            anel = CriarCircular(0.78f, "RuntimeAnel", out _);
            return anel;
        }

        /// <summary>
        /// Desenha um circulo com suavizacao nas bordas. Com raioInterno &gt; 0 o
        /// miolo fica vazado e sobra apenas um anel.
        /// </summary>
        private static Sprite CriarCircular(float raioInterno, string nome, out Texture2D textura)
        {
            const int tam = 128;
            const float meio = tam / 2f;
            var tex = NovaTextura(tam);
            var px = new Color32[tam * tam];

            for (int y = 0; y < tam; y++)
            {
                for (int x = 0; x < tam; x++)
                {
                    float dx = (x + 0.5f - meio) / meio;
                    float dy = (y + 0.5f - meio) / meio;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    // Suavizacao de ~1,5 pixel para a borda nao ficar serrilhada.
                    float suave = 1.5f / meio;
                    float a = Mathf.Clamp01((1f - d) / suave);
                    if (raioInterno > 0f) a = Mathf.Min(a, Mathf.Clamp01((d - raioInterno) / suave));

                    px[y * tam + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            textura = tex;
            return Criar(tex, nome);
        }

        private static Texture2D NovaTextura(int tam)
        {
            var tex = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }

        private static Sprite Criar(Texture2D tex, string nome)
        {
            // pixelsPerUnit igual ao lado da textura: 1 unidade de mundo = sprite
            // inteiro, entao a escala do Transform vira o diametro em unidades.
            var s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                  new Vector2(0.5f, 0.5f), tex.width);
            s.name = nome;
            s.hideFlags = HideFlags.HideAndDontSave;
            return s;
        }
    }
}
