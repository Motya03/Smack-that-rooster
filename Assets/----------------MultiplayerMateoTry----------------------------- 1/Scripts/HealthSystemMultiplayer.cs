using UnityEngine;
using UnityEngine.UI;

public class HealthSystemMultiplayer : MonoBehaviour
{
    [Header("Vida (UI)")]
    public int maxHealth = 3;
   


    [HideInInspector] public int health;

    [Header("Referencias UI")]
    public Image[] hearts;         // arrastrar las 3 Image hijos en este UI
    public Sprite fullHeart;      // sprite "corazón lleno" (color correspondiente)
    public Sprite emptyHeart;     // sprite "corazón vacío"  (mismo color)
    public HeartFlash heartFlash; // componente HeartFlash en este mismo GameObject (opcional)

    private void Awake()
    {
        // Inicializa la UI con full hearts por defecto (si se han asignado las imágenes)
        health = maxHealth;
        UpdateHearts();
    }

    public void TakeDamage(int amount)
    {
        
        
        health -= amount;
        health = Mathf.Clamp(health, 0, maxHealth);
        UpdateHearts();

        if (heartFlash != null)
            heartFlash.FlashHearts();

        

    }

    public void Heal(int amount)
    {
        health += amount;
        health = Mathf.Clamp(health, 0, maxHealth);
        UpdateHearts();
    }

    public void ResetHealth()
    {
        health = maxHealth;
        UpdateHearts();
    }

    private void UpdateHearts()
    {
        if (hearts == null) return;
        for (int i = 0; i < hearts.Length; i++)
        {
            if (i < health)
                hearts[i].sprite = fullHeart;
            else
                hearts[i].sprite = emptyHeart;
        }
    }
}



