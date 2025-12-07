using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ServerPlayerSpawnPoints : MonoBehaviour
{
    public static ServerPlayerSpawnPoints Instance { get; private set; }

    [SerializeField]
    private List<GameObject> m_SpawnPoints;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public GameObject GetRandomSpawnPoint()
    {
        if (m_SpawnPoints.Count == 0)
            return null;
        return m_SpawnPoints[Random.Range(0, m_SpawnPoints.Count)];
    }
}
