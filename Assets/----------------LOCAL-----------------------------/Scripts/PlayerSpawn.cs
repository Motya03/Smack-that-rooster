using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerSpawn : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    private List<int> usedIndexes = new List<int>();

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        StartCoroutine(PlacePlayerNextFrame(playerInput));
    }

    private IEnumerator PlacePlayerNextFrame(PlayerInput playerInput)
    {
        // Esperar un frame completo para que el CharacterController esté listo
        yield return new WaitForEndOfFrame();

        if (spawnPoints.Length == 0)
        {
            Debug.LogWarning("No hay puntos de spawn asignados en PlayerSpawn.");
            yield break;
        }

        // Escoger punto de spawn aleatorio
        int randomIndex = GetUniqueRandomIndex();
        Transform spawn = spawnPoints[randomIndex];

        // Mover directamente sin desactivar el CharacterController
        CharacterController cc = playerInput.GetComponent<CharacterController>();
        if (cc != null)
        {
            // Mover manualmente al punto sin usar .Move()
            cc.enabled = false;
            playerInput.transform.position = spawn.position;
            playerInput.transform.rotation = spawn.rotation;
            cc.enabled = true;
        }
        else
        {
            // Si no hay CharacterController, mover el transform directamente
            playerInput.transform.position = spawn.position;
            playerInput.transform.rotation = spawn.rotation;
        }

        Debug.Log($"Jugador {playerInput.playerIndex} spawneado en {spawn.name}");
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

}

