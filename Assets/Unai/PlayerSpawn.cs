using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawn : MonoBehaviour
{

    public Transform[] SpawnPoints;
    private int m_playerCount;
    
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        playerInput.transform.position = SpawnPoints[m_playerCount].transform.position;
        /*
        if (m_playerCount == 0)
        {
            playerInput.GetComponent<PlayerController>().SwitchOnCamera();
        }
        */
        m_playerCount++;
    }
}
