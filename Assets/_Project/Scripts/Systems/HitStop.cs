using System.Collections;
using UnityEngine;

namespace EcosDeAldenor.Systems
{
    /// <summary>
    /// "Hit stop" (ou hit freeze): congela o tempo por uma fracao de segundo no
    /// momento do impacto, dando peso e impacto aos golpes - tecnica classica de
    /// game feel em jogos de acao. Usa tempo NAO escalado para se restaurar, entao
    /// funciona mesmo com Time.timeScale reduzido. Instancia-se sob demanda.
    /// </summary>
    public class HitStop : MonoBehaviour
    {
        private static HitStop instance;
        private Coroutine running;

        /// <summary>
        /// Congela o jogo por 'duration' segundos reais, reduzindo o Time.timeScale.
        /// </summary>
        public static void Do(float duration = 0.05f, float frozenScale = 0.04f)
        {
            // Nao dispara se o jogo estiver pausado (timeScale ja zerado pelo menu).
            if (Time.timeScale <= 0.001f) return;

            EnsureInstance();
            if (instance.running != null) instance.StopCoroutine(instance.running);
            instance.running = instance.StartCoroutine(instance.Freeze(duration, frozenScale));
        }

        private static void EnsureInstance()
        {
            if (instance != null) return;
            var go = new GameObject("HitStop");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<HitStop>();
        }

        private IEnumerator Freeze(float duration, float frozenScale)
        {
            Time.timeScale = frozenScale;
            yield return new WaitForSecondsRealtime(duration);
            // So restaura se ninguem pausou o jogo nesse meio tempo.
            if (Time.timeScale <= frozenScale + 0.001f) Time.timeScale = 1f;
            running = null;
        }
    }
}
