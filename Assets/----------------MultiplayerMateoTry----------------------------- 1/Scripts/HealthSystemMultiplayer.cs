using UnityEngine;
using UnityEngine.UI;

public class HealthSystemMultiplayer : MonoBehaviour
{
    public int maxHealth = 3;
    public int health;

    public Image[] hearts;
    public Sprite fullHeart;
    public Sprite emptyHeart;

    private void Awake()
    {
        health = maxHealth;
        UpdateHearts();
    }

    public void TakeDamage(int amount)
    {
        health = Mathf.Clamp(health - amount, 0, maxHealth);
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
            hearts[i].sprite = i < health ? fullHeart : emptyHeart;
        }
    }

    // <<< AGREGAR ESTO >>>
    public void RefreshHeartsFromNetwork()
    {
        UpdateHearts();
    }
}
