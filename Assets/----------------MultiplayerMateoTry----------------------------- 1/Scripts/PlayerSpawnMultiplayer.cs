using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerSpawnMultiplayer : NetworkBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    private List<int> usedIndexes = new List<int>();

    [SerializeField] private Material[] outlineMaterials;

    public static List<GameObject> joinedPlayers = new List<GameObject>();

    public override void OnNetworkSpawn()
    {
        // Solo el servidor coloca jugadores
        if (IsServer)
        {
            PlacePlayerServerRpc();
        }

        // Solo el dueño controla su personaje
        TogglePlayerControl(IsOwner);
    }

    [ServerRpc]
    private void PlacePlayerServerRpc(ServerRpcParams rpcParams = default)
    {
        int index = GetUniqueRandomIndex();
        Transform spawn = spawnPoints[index];

        // Reposicionar al jugador
        transform.position = spawn.position;
        transform.rotation = spawn.rotation;

        ApplyOutlineMaterialClientRpc(OwnerClientId, index);

        if (!joinedPlayers.Contains(gameObject))
            joinedPlayers.Add(gameObject);
    }

    private int GetUniqueRandomIndex()
    {
        if (usedIndexes.Count >= spawnPoints.Length)
            usedIndexes.Clear();

        int randomIndex;
        do
        {
            randomIndex = Random.Range(0, spawnPoints.Length);
        }
        while (usedIndexes.Contains(randomIndex));

        usedIndexes.Add(randomIndex);
        return randomIndex;
    }

    [ClientRpc]
    private void ApplyOutlineMaterialClientRpc(ulong clientId, int materialIndex)
    {
        if (materialIndex >= outlineMaterials.Length)
            return;

        Renderer renderer = GetComponentInChildren<Renderer>();

        if (renderer != null)
        {
            List<Material> mats = new List<Material>(renderer.sharedMaterials);

            if (mats.Count == 1)
                mats.Add(outlineMaterials[materialIndex]);
            else
                mats[1] = outlineMaterials[materialIndex];

            renderer.materials = mats.ToArray();
        }
    }

    private void TogglePlayerControl(bool state)
    {
        // Solo el dueño controla inputs
        foreach (var comp in GetComponentsInChildren<MonoBehaviour>())
        {
            // Evita desactivar scripts de Netcode
            if (comp is NetworkBehaviour) continue;

            comp.enabled = state;
        }
    }
}
