using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerSpawn : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    private List<int> usedIndexes = new List<int>();

    // 🔹 AHORA ES UNA LISTA DE GAMEOBJECTS
    public static List<GameObject> joinedPlayers = new List<GameObject>();

    [SerializeField] private Material[] outlineMaterials;

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        StartCoroutine(PlacePlayerNextFrame(playerInput));

        FindFirstObjectByType<LobbyJoinManager>()?
            .OnPlayerJoinedVisual(playerInput.playerIndex);
    }

    private IEnumerator PlacePlayerNextFrame(PlayerInput playerInput)
    {
        yield return new WaitForEndOfFrame();

        int randomIndex = GetUniqueRandomIndex();
        Transform spawn = spawnPoints[randomIndex];

        CharacterController cc = playerInput.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            playerInput.transform.position = spawn.position;
            playerInput.transform.rotation = spawn.rotation;
            cc.enabled = true;
        }
        else
        {
            playerInput.transform.position = spawn.position;
            playerInput.transform.rotation = spawn.rotation;
        }

        // 🔹 Guardar el jugador como GameObject
        if (!joinedPlayers.Contains(playerInput.gameObject))
            joinedPlayers.Add(playerInput.gameObject);

        // 🔹 Desactivar control
        TogglePlayerControl(playerInput.gameObject, false);

        Debug.Log($"Jugador {playerInput.playerIndex} spawneado en {spawn.name}");

        // Asignar outline
        Transform meshChild = playerInput.transform.Find("Sphere.001");
        if (meshChild != null)
        {
            Renderer renderer = meshChild.GetComponent<Renderer>();
            if (renderer != null)
            {
                List<Material> mats = new List<Material>(renderer.sharedMaterials);

                if (mats.Count == 1)
                    mats.Add(outlineMaterials[playerInput.playerIndex]);
                else
                    mats[1] = outlineMaterials[playerInput.playerIndex];

                renderer.materials = mats.ToArray();
            }
        }
        /* // 🔹 ASIGNAR EL CANVAS JUSTO DESPUÉS DEL SPAWN
         var mov = playerInput.GetComponent<PlayerMovLocal>();
         if (mov != null)
             mov.SetCanvasEscape(GameObject.FindWithTag("PauseCanvas"));
         */
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

    // 🔹 AHORA RECIBE GAMEOBJECT
    public static void TogglePlayerControl(GameObject playerObj, bool state)
    {
        if (!playerObj) return;

        foreach (var comp in playerObj.GetComponentsInChildren<MonoBehaviour>())
        {
            if (comp is PlayerInput) continue; // nunca desactivar PlayerInput
            comp.enabled = state;
        }
    }
}
