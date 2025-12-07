using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class ServerPlayerSpawnPoints : NetworkBehaviour
{
    public static ServerPlayerSpawnPoints Instance { get; private set; }

    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    private List<int> availableIndices = new List<int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            InitializeAvailableIndices();
        }
    }

    private void InitializeAvailableIndices()
    {
        availableIndices.Clear();
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            availableIndices.Add(i);
        }
    }

    public Transform GetRandomSpawnPoint()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points assigned!");
            return null;
        }

        if (availableIndices.Count == 0)
        {
            Debug.LogWarning("No available spawn points, resetting...");
            InitializeAvailableIndices();
        }

        int randomIndex = Random.Range(0, availableIndices.Count);
        int spawnPointIndex = availableIndices[randomIndex];
        availableIndices.RemoveAt(randomIndex);

        return spawnPoints[spawnPointIndex];
    }

    public void ReturnSpawnPoint(Transform spawnPoint)
    {
        int index = spawnPoints.IndexOf(spawnPoint);
        if (index != -1 && !availableIndices.Contains(index))
        {
            availableIndices.Add(index);
        }
    }

    public Transform GetSpawnPointByIndex(int index)
    {
        if (index >= 0 && index < spawnPoints.Count)
        {
            return spawnPoints[index];
        }
        return null;
    }

    public Vector3 GetRandomSpawnPosition()
    {
        Transform spawnPoint = GetRandomSpawnPoint();
        return spawnPoint != null ? spawnPoint.position : Vector3.zero;
    }

    public void AddSpawnPoint(Transform newSpawnPoint)
    {
        if (!spawnPoints.Contains(newSpawnPoint))
        {
            spawnPoints.Add(newSpawnPoint);
            availableIndices.Add(spawnPoints.Count - 1);
        }
    }

    public void RemoveSpawnPoint(Transform spawnPointToRemove)
    {
        int index = spawnPoints.IndexOf(spawnPointToRemove);
        if (index != -1)
        {
            spawnPoints.RemoveAt(index);
            availableIndices.Remove(index);

            // Re-index available indices
            for (int i = 0; i < availableIndices.Count; i++)
            {
                if (availableIndices[i] > index)
                {
                    availableIndices[i]--;
                }
            }
        }
    }
}