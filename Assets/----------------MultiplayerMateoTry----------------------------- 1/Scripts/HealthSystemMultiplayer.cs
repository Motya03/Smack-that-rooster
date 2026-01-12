using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class HealthSystemMultiplayer : NetworkBehaviour
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

    public void ResetHealthFromNetwork()
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
