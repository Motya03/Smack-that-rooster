using Unity.Netcode;
using UnityEngine;

public class PlayerHealthMultiplayer : NetworkBehaviour
{
    [Header("Configuración")]
    [SerializeField] private int maxHealth = 3;

    // --- CORRECCIÓN AQUÍ ---
    public NetworkVariable<int> NetworkHealth = new NetworkVariable<int>(
        value: 3,
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server
    );
    // -----------------------

    // Referencia a la UI local
    [HideInInspector] public HealthSystemMultiplayer uiHealth;

    // Datos locales
    [HideInInspector] public bool isDefinitivelyDead = false;
    private PlayerMovMultiplayer lastAttackerScript = null;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
            NetworkHealth.Value = maxHealth;

        // Sincronizar la UI local cuando cambie la vida en la NetworkVariable
        NetworkHealth.OnValueChanged += (oldValue, newValue) =>
        {
            if (uiHealth != null)
            {
                uiHealth.health = newValue;
                uiHealth.RefreshHeartsFromNetwork();
            }
        };

        if (uiHealth != null)
        {
            uiHealth.health = NetworkHealth.Value;
            uiHealth.RefreshHeartsFromNetwork();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage, ServerRpcParams rpcParams = default)
    {
        ProcessDamageOnServer(damage, rpcParams.Receive.SenderClientId);
    }

    public void ProcessDamageOnServer(int damage, ulong attackerClientId)
    {
        if (!IsServer) return;

        int newHealth = Mathf.Clamp(NetworkHealth.Value - damage, 0, maxHealth);
        NetworkHealth.Value = newHealth;

        TakeHitClientRpc(attackerClientId);

        if (newHealth <= 0)
        {
            // Lógica de muerte
        }
    }

    [ClientRpc]
    private void TakeHitClientRpc(ulong attackerClientId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(attackerClientId, out NetworkObject attackerObj))
            lastAttackerScript = attackerObj.GetComponent<PlayerMovMultiplayer>();

        if (uiHealth != null)
        {
            var hf = uiHealth.GetComponent<HeartFlashMultiplayer>();
            if (hf != null) hf.FlashHearts();
        }

        // SoundManager.PlaySound(SoundType.HitCulo);
    }

    [ServerRpc(RequireOwnership = false)]
    public void HealServerRpc(int amount, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        NetworkHealth.Value = Mathf.Clamp(NetworkHealth.Value + amount, 0, maxHealth);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetHealthServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        NetworkHealth.Value = maxHealth;
    }
}