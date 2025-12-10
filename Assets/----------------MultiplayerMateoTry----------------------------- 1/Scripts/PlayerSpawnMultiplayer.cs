using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawnMultiplayer : NetworkBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    private static List<int> usedIndexes = new List<int>();

    [SerializeField] private Material[] outlineMaterials;

    // Lista local por máquina: en cada cliente se llena con SUS instancias de players
    public static List<GameObject> joinedPlayers = new List<GameObject>();

    public override void OnNetworkSpawn()
    {
        // 🔹 Cada vez que un Player se spawnea en esta máquina, lo registramos
        if (!joinedPlayers.Contains(gameObject))
            joinedPlayers.Add(gameObject);

        // 🔹 Solo el server decide posición / material
        if (IsServer)
            StartCoroutine(DelayedSpawn());
    }

    private IEnumerator DelayedSpawn()
    {
        // Esperar 1 frame para que el NetworkTransform se inicialice
        yield return null;
        PlacePlayer();
    }

    private void PlacePlayer()
    {
        int index = GetUniqueRandomIndex();
        Transform spawn = spawnPoints[index];

        transform.position = spawn.position;
        transform.rotation = spawn.rotation;

        ApplyOutlineMaterialClientRpc(index);

        // 🔸 YA NO añadimos aquí a joinedPlayers porque se hace en OnNetworkSpawn
        // if (!joinedPlayers.Contains(gameObject))
        //     joinedPlayers.Add(gameObject);
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
    private void ApplyOutlineMaterialClientRpc(int materialIndex)
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

    public static void TogglePlayerControl(GameObject playerObj, bool state)
    {
        if (!playerObj) return;

        foreach (var comp in playerObj.GetComponentsInChildren<MonoBehaviour>())
        {
            if (comp is UnityEngine.InputSystem.PlayerInput) continue;
            comp.enabled = state;
        }
    }
}
