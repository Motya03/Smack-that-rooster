using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerSpawn : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    private List<int> usedIndexes = new List<int>();

    // 🔹 Guardaremos todos los jugadores que se unan
    public static List<PlayerInput> joinedPlayers = new List<PlayerInput>();

    [SerializeField] private Material[] outlineMaterials;


    public void OnPlayerJoined(PlayerInput playerInput)
    {
        StartCoroutine(PlacePlayerNextFrame(playerInput));
        FindFirstObjectByType<LobbyJoinManager>()?.OnPlayerJoinedVisual(playerInput.playerIndex);

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

        // 🔹 Guardar el jugador para después
        if (!joinedPlayers.Contains(playerInput))
            joinedPlayers.Add(playerInput);

        // 🔹 Desactivar sus scripts de control por ahora
        TogglePlayerControl(playerInput, false);

        Debug.Log($"Jugador {playerInput.playerIndex} spawneado en {spawn.name}");


        // 🔹 Buscar el mesh dentro del prefab (por nombre o por tipo)
        Transform meshChild = playerInput.transform.Find("Sphere.001");
        if (meshChild != null)
        {
            // 🔹 Buscar el componente de render (MeshRenderer o SkinnedMeshRenderer)
            Renderer renderer = meshChild.GetComponent<Renderer>();
            if (renderer != null)
            {
                List<Material> mats = new List<Material>(renderer.sharedMaterials);

                if (mats.Count == 1)
                    mats.Add(outlineMaterials[playerInput.playerIndex]);
                else
                    mats[1] = outlineMaterials[playerInput.playerIndex];

                renderer.materials = mats.ToArray();

                Debug.Log($"🟢 Asignado outline {outlineMaterials[playerInput.playerIndex].name} al jugador {playerInput.playerIndex}");
            }
            else
            {
                Debug.LogWarning($"⚠️ No se encontró Renderer en {meshChild.name}");
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ No se encontró el objeto 'Sphere.001' en el prefab del jugador {playerInput.playerIndex}");
        }


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

    // 🔹 Método auxiliar para (des)activar control
    public static void TogglePlayerControl(PlayerInput player, bool state)
    {
        foreach (var comp in player.GetComponentsInChildren<MonoBehaviour>())
        {
            // Evita desactivar el propio PlayerInput
            if (comp is PlayerInput) continue;

            // Ejemplo: desactivar scripts de movimiento, ataque, etc.
            comp.enabled = state;
        }
    }
}


