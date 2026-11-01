using UnityEngine;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// Sons gerados em codigo, no mesmo espirito do RuntimeSprites: quando
    /// nenhum pacote do projeto traz o som, ele e sintetizado em memoria em vez
    /// de entrar um arquivo novo de fonte desconhecida.
    ///
    /// Hoje e so o SINO. A historia inteira gira em torno dos sinos de Aldenor
    /// ("os sinos se calaram", "os sinos tocaram outra vez") e nenhum dos
    /// pacotes de audio tem um.
    /// </summary>
    public static class RuntimeAudio
    {
        private static AudioClip sino;

        /// <summary>
        /// Badalada de sino de igreja: soma de parciais que decaem em tempos
        /// diferentes. As razoes sao as de um sino afinado de verdade (hum uma
        /// oitava abaixo, a nota, a terca menor, a quinta, a oitava...) - e a
        /// terca menor que faz soar "sino" e nao "diapasao". O agudo morre
        /// primeiro, entao a cauda fica grave e longa, como deve.
        /// </summary>
        public static AudioClip Sino()
        {
            if (sino != null) return sino;

            const int taxa = 44100;
            const float duracao = 4f;
            const float nota = 196f;   // sol grave: sino de torre, nao sininho
            int n = Mathf.CeilToInt(taxa * duracao);

            // razao da frequencia, amplitude, tempo de queda (s)
            float[,] parciais =
            {
                { 0.5f,   0.35f, 3.2f },
                { 1f,     1f,    2.6f },
                { 1.183f, 0.6f,  1.9f },
                { 1.506f, 0.4f,  1.4f },
                { 2f,     0.5f,  1.2f },
                { 2.514f, 0.25f, 0.8f },
                { 2.662f, 0.2f,  0.7f },
                { 3.011f, 0.15f, 0.5f },
            };

            var amostras = new float[n];
            var aleatorio = new System.Random(7);
            float pico = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)taxa;
                float v = 0f;
                for (int p = 0; p < parciais.GetLength(0); p++)
                    v += parciais[p, 1] * Mathf.Exp(-t / parciais[p, 2]) *
                         Mathf.Sin(2f * Mathf.PI * nota * parciais[p, 0] * t);

                // A batida do badalo: um estalo curto de ruido no comeco.
                if (t < 0.02f) v += (float)(aleatorio.NextDouble() * 2.0 - 1.0) * 0.5f * (1f - t / 0.02f);

                // Entrada de 3 ms para o comeco nao estalar no alto-falante.
                v *= Mathf.Clamp01(t / 0.003f);
                amostras[i] = v;
                pico = Mathf.Max(pico, Mathf.Abs(v));
            }

            float ganho = pico > 0f ? 0.8f / pico : 1f;
            for (int i = 0; i < n; i++) amostras[i] *= ganho;

            sino = AudioClip.Create("RuntimeSino", n, 1, taxa, false);
            sino.SetData(amostras, 0);
            return sino;
        }
    }
}
