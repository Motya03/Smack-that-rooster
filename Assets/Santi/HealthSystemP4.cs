using UnityEngine;
using UnityEngine.UI;

public class HealthSystemP4 : MonoBehaviour
{
    public int health = 3;           // Vida actual
    public int maxHealth = 3;        // Vida máxima

    public Image[] hearts;           // Array de imágenes de corazones
    public Sprite fullHeart;
    public Sprite emptyHeart;

    void Update()
    {
        // Asegurar que la vida no sobrepase límites
        health = Mathf.Clamp(health, 0, maxHealth);

        // Actualizar cada corazón
        for (int i = 0; i < hearts.Length; i++)
        {
            if (i < health)
                hearts[i].sprite = fullHeart;
            else
                hearts[i].sprite = emptyHeart;
        }
        if (Input.GetKeyDown(KeyCode.P))
            TakeDamage(1);
    }

    // Funciones para probar
    public void TakeDamage(int amount)
    {
        health -= amount;
        heartFlash.FlashHearts();
    }

    public HeartFlash heartFlash;
}
