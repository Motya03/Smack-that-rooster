using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class HealthSystemMulti : NetworkBehaviour
{
    [Header("Vida (UI)")]
    public int maxHealth = 3;
    private NetworkVariable<int> networkHealth = new NetworkVariable<int>(3);

    [Header("Referencias UI")]
    public Image[] hearts;
    public Sprite fullHeart;
    public Sprite emptyHeart;
    public HeartFlash heartFlash;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            networkHealth.Value = maxHealth;
        }

        networkHealth.OnValueChanged += OnHealthChanged;
        UpdateHearts();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int amount)
    {
        int newHealth = networkHealth.Value - amount;
        networkHealth.Value = Mathf.Clamp(newHealth, 0, maxHealth);

        TakeDamageClientRpc(amount);
    }

    [ClientRpc]
    private void TakeDamageClientRpc(int amount)
    {
        UpdateHearts();

        if (heartFlash != null)
            heartFlash.FlashHearts();
    }

    [ServerRpc(RequireOwnership = false)]
    public void HealServerRpc(int amount)
    {
        int newHealth = networkHealth.Value + amount;
        networkHealth.Value = Mathf.Clamp(newHealth, 0, maxHealth);

        HealClientRpc(amount);
    }

    [ClientRpc]
    private void HealClientRpc(int amount)
    {
        UpdateHearts();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetHealthServerRpc()
    {
        networkHealth.Value = maxHealth;
        ResetHealthClientRpc();
    }

    [ClientRpc]
    private void ResetHealthClientRpc()
    {
        UpdateHearts();
    }

    private void OnHealthChanged(int oldHealth, int newHealth)
    {
        UpdateHearts();
    }

    private void UpdateHearts()
    {
        if (hearts == null) return;

        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null) continue;

            if (i < networkHealth.Value)
                hearts[i].sprite = fullHeart;
            else
                hearts[i].sprite = emptyHeart;
        }
    }

    // Local method for UI updates (called by PlayerMovMultiplayer)
    public void TakeDamage(int amount)
    {
        // This is only for local UI updates
        UpdateHearts();

        if (heartFlash != null)
            heartFlash.FlashHearts();
    }

    public void ResetHealth()
    {
        // This is only for local UI updates
        UpdateHearts();
    }

    public int health => networkHealth.Value;

    public override void OnDestroy()
    {
        base.OnDestroy();
        networkHealth.OnValueChanged -= OnHealthChanged;
    }
}