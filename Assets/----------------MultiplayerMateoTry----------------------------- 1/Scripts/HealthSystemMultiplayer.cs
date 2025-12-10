using UnityEngine;
using UnityEngine.UI;

public class HealthSystemMultiplayer : MonoBehaviour
{
    [Header("Configuración UI")]
    public int maxHealth = 3;

    [HideInInspector] public int health;

    [Header("Referencias UI")]
    public Image[] hearts;         // arrastra aquí las Image (ordenadas)
    public Sprite fullHeart;      // sprite corazón lleno
    public Sprite emptyHeart;     // sprite corazón vacío

    [Header("Flash (opcional)")]
    public HeartFlashMultiplayer heartFlash;
    public void FlashHearts()
    {
        if (heartFlash != null)
            heartFlash.FlashHearts();
    }

    private void Awake()
    {
        health = maxHealth;
        UpdateHearts();
    }

    public void TakeDamage(int amount)
    {
        health = Mathf.Clamp(health - amount, 0, maxHealth);
        UpdateHearts();
        if (heartFlash != null) heartFlash.FlashHearts();
    }

    public void Heal(int amount)
    {
        health = Mathf.Clamp(health + amount, 0, maxHealth);
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
    public void SetHealth(int value)
    {
        health = Mathf.Clamp(value, 0, maxHealth);
        UpdateHearts();
    }

    // Método público para que el Player actualice la UI cuando NetworkVariable cambie
    public void RefreshHeartsFromNetwork()
    {
        UpdateHearts();
    }
}
