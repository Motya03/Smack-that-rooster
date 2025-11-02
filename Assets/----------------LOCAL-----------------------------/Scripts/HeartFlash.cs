using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class HeartFlash : MonoBehaviour
{
    public Image[] hearts;
    public float flashDuration = 0.1f;
    public int flashCount = 3;

    public void FlashHearts()
    {
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        for (int i = 0; i < flashCount; i++)
        {
            foreach (var heart in hearts)
                heart.enabled = false; // Oculta momentáneamente

            yield return new WaitForSeconds(flashDuration);

            foreach (var heart in hearts)
                heart.enabled = true; // Vuelve a mostrar

            yield return new WaitForSeconds(flashDuration);
        }
    }
}
