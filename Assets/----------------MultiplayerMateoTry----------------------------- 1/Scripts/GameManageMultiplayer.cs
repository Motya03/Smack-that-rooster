using System.Collections;
using System.Collections.Generic;

//using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameManageMultiplayer : NetworkBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TimerMultiplayer timer;
    [SerializeField] private GameObject[] playerPopups;
    [SerializeField] private Text winnerText;
    [SerializeField] private GameObject canvasWinner;

    // No necesitamos guardar 'enemy' como variable global si lo buscamos al momento, 
    // pero lo dejaré si lo usas en otro lado.
    private GameObject enemy;

    private bool throwCageBool = true;
    private Transform enemyPoint;

    [Header("Prefab")]
    [SerializeField] private GameObject Box; // ¡Este prefab debe tener un NetworkObject!

    [Header("Canvas Gameplay")]
    public GameObject canvasLocal;

    private bool gameEnded = false;
    public bool gameStarted = false;
    private bool clickerActive = false;

    private void Update()
    {
        // Solo permitimos que el jugador local presione la tecla
        // Si este script está en un objeto de la escena (no en el jugador), 
        // cualquiera puede llamar al Update, así que cuidado.
       
    }

    public void PrepareCage()
    {
        if (throwCageBool && IsServer)
        {
            // En lugar de llamar a una corrutina local o ClientRpc, 
            // pedimos al Servidor que tire la caja.
            RequestThrowCageServerRpc();

            // Opcional: Bloquear para no spamear
            // throwCageBool = false; 
        }
    }

    // [ServerRpc] indica que este código se ejecutará EN EL SERVIDOR,
    // aunque lo llame un cliente.
    // RequireOwnership = false permite que cualquier cliente llame a este RPC 
    // (necesario si el GameManager es propiedad del Host/Server).
    [ServerRpc(RequireOwnership = false)]
    private void RequestThrowCageServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // El servidor inicia la corrutina
        StartCoroutine(ServerThrowCageCoroutine());
    }

    private IEnumerator ServerThrowCageCoroutine()
    {
        // Esperamos el segundo que querías
        yield return new WaitForSeconds(1f);

        // Buscamos al enemigo (Lógica ejecutada en el Servidor)
        GameObject target = FindEnemyLogic();

        if (target == null)
        {
            Debug.LogWarning("❌ [Server] No hay enemigo para tirar la caja");
            yield break;
        }

        Vector3 spawnPos = target.transform.position + Vector3.up * 10f;

        // 1. INSTANCIAR (Solo ocurre en el servidor)
        GameObject cajaInstance = Instantiate(Box, spawnPos, Quaternion.identity);

        // 2. SPAWNEAR (Esto es lo que hace que se vea en todos los clientes)
        NetworkObject netObj = cajaInstance.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
            Debug.Log("🔥 Caja spawneada en red correctamente");
        }
        else
        {
            Debug.LogError("❌ El prefab de la caja NO tiene el componente NetworkObject");
        }
    }

    // He separado la lógica de buscar enemigo para que devuelva el objeto
    public GameObject FindEnemyLogic()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players.Length == 0) return null;

        // Aquí puedes mejorar la lógica para que no se tire la caja a sí mismo
        // pero por ahora uso tu lógica aleatoria.
        GameObject selectedEnemy = players[Random.Range(0, players.Length)];

        // Actualizamos la variable global por si la usas en otro lado
        enemy = selectedEnemy;

        return selectedEnemy;
    }

    private void Start()
    {
        Debug.Log("🔥 [GM] Start ejecutado");
    }

    // ... Resto de tu código (SetClickerState, ActivateGame, etc) se mantiene igual ...

    public void SetClickerState(bool state)
    {
        clickerActive = state;
    }

    public void ActivateGame()
    {
        if (gameStarted) return;
        gameStarted = true;

        if (timer != null)
        {
            timer.ResetTimer();
            timer.StartTimer();
        }
        if (canvasLocal != null) canvasLocal.SetActive(true);
    }
    public void CheckRemainingPlayers()
    {
        Debug.Log("🧮 [GM] CheckRemainingPlayers llamado");
    }
    public void TimeEnded()
    {
        if (canvasLocal != null) canvasWinner.SetActive(true);
        //if (gameEnded || clickerActive)
        //  return;

        EndGame(GetWinnerIndexByHealth());

    }
    private void EndGame(int winnerIndex)
    {
       // if (gameEnded) return;

       // gameEnded = true;

        // Desactivar control de jugadores
        foreach (var obj in PlayerSpawnMultiplayer.joinedPlayers)
            PlayerSpawnMultiplayer.TogglePlayerControl(obj, false);

        // Apagar popups individuales
        foreach (var p in playerPopups)
            p.SetActive(false);

        // Mensaje ganador
        string msg = $"Ganador: Jugador {winnerIndex + 1}";

        // Mostrar popup del ganador
        if (winnerIndex >= 0 && winnerIndex < playerPopups.Length)
            playerPopups[winnerIndex].SetActive(true);

        // 🔥 DESACTIVAR HUD LOCAL
        if (canvasLocal != null)
            canvasLocal.SetActive(false);

        // 🔥 ACTIVAR CANVAS GANADOR
        if (winnerText != null)
            winnerText.text = msg;

        if (canvasWinner != null)
            canvasWinner.SetActive(true);

        Debug.Log("🎉 FIN DE PARTIDA → " + msg);
        MusicManager.StopMusic(MusicType.MainMenuBack);
        MusicManager.StopMusic(MusicType.FightMusic);
        MusicManager.StopMusic(MusicType.ChickenMusic);
        MusicManager.StopMusic(MusicType.ClickerGameMusic);
        MusicManager.PlayMusic(MusicType.EnterCharMusic, 0.5f);
    }
   

   

    // -----------------------------------------------------
    // ❤️ Ganador por salud (sin empates)
    // -----------------------------------------------------
    private int GetWinnerIndexByHealth()
    {
        int maxHealth = -1;
        List<int> candidates = new List<int>();

        for (int i = 0; i < PlayerSpawn.joinedPlayers.Count; i++)
        {
            GameObject obj = PlayerSpawn.joinedPlayers[i];
            if (!obj) continue;

            PlayerMovMultiplayer p = obj.GetComponent<PlayerMovMultiplayer>();
            if (!p || p.isDefinitivelyDead || p.uiHealth == null) continue;

            int h = p.uiHealth.health;

            if (h > maxHealth)
            {
                maxHealth = h;
                candidates.Clear();
                candidates.Add(i);
            }
            else if (h == maxHealth)
            {
                candidates.Add(i);
            }
        }

        // 🔥 Si hay empate → elegir uno aleatorio (sin empates finales)
        if (candidates.Count > 1)
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];


        return candidates.Count == 1 ? candidates[0] : 0;
    }
}