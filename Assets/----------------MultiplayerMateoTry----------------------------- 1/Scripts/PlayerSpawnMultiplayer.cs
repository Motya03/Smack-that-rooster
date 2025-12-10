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

    public static List<GameObject> joinedPlayers = new List<GameObject>();


    public override void OnNetworkSpawn()
    {
        if (IsServer)
            StartCoroutine(DelayedSpawn());
        if (IsOwner)
        {
            // El dueño controla su player
            GetComponent<PlayerInput>().enabled = true;
        }
        else
        {
            // Los demás NO deben controlar al player, pero sus scripts de movimiento
            // deben seguir activos para que NetworkTransform los sincronice
            GetComponent<PlayerInput>().enabled = false;
        }

    }

    private IEnumerator DelayedSpawn()
    {
        yield return null; // Esperar 1 frame para que el NetworkTransform se inicialice
        PlacePlayer();
    }


        
    

    private void PlacePlayer()
    {
        int index = GetUniqueRandomIndex();
        Transform spawn = spawnPoints[index];

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
