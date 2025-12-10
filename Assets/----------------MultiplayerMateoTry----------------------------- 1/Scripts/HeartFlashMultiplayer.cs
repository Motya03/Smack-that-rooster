using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HeartFlashMultiplayer : MonoBehaviour
{
    public Image[] hearts;
    public float flashDuration = 0.08f;
    public int flashCount = 3;

    public void FlashHearts()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < flashCount; i++)
        {
            foreach (var heart in hearts)
                if (heart != null) heart.enabled = false;

            yield return new WaitForSeconds(flashDuration);

            foreach (var heart in hearts)
                if (heart != null) heart.enabled = true;

            yield return new WaitForSeconds(flashDuration);
        }
    }
}
