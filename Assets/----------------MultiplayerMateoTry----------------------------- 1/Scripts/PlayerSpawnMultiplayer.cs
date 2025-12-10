using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawnMultiplayer : NetworkBehaviour
{
    private static List<int> usedIndexes = new List<int>();

    [Header("Outline / color (opcional)")]
    [SerializeField] private Material[] outlineMaterials;

    public static List<GameObject> joinedPlayers = new List<GameObject>();

    private Transform[] spawnPoints;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Obtener spawn points desde el manager (si existe)
        if (SpawnManager.Instance != null)
            spawnPoints = SpawnManager.Instance.spawnPoints;

        // El servidor posiciona
        if (IsServer)
            StartCoroutine(DelayedSpawn());

        // Solo el dueño debe procesar input localmente (no desactivar otros componentes)
        if (IsOwner)
            EnableLocalInput(true);
        else
            EnableLocalInput(false);
    }

    private IEnumerator DelayedSpawn()
    {
        // Esperar 1 frame para asegurar que NetworkTransform/NetworkObject esté listo en clientes
        yield return null;

        PlacePlayerOnServer();
    }

    private void PlacePlayerOnServer()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points asignados en SpawnManager.");
            return;
        }

        int index = GetUniqueRandomIndex();
        Transform spawn = spawnPoints[index];

        // Solo el server escribe transform
        transform.position = spawn.position;
        transform.rotation = spawn.rotation;

        ApplyOutlineMaterialClientRpc(index);

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
        } while (usedIndexes.Contains(randomIndex));

        usedIndexes.Add(randomIndex);
        return randomIndex;
    }

    [ClientRpc]
    private void ApplyOutlineMaterialClientRpc(int materialIndex)
    {
        if (outlineMaterials == null || outlineMaterials.Length == 0) return;
        if (materialIndex < 0 || materialIndex >= outlineMaterials.Length) return;

        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer == null) return;

        var mats = new System.Collections.Generic.List<Material>(renderer.sharedMaterials);
        if (mats.Count == 1)
            mats.Add(outlineMaterials[materialIndex]);
        else
            mats[1] = outlineMaterials[materialIndex];

        renderer.materials = mats.ToArray();
    }

    private void EnableLocalInput(bool enable)
    {
        // Activa/desactiva solo el PlayerInput para evitar desactivar otros sistemas necesarios para sincronización.
        var playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null)
            playerInput.enabled = enable;
    }
}
