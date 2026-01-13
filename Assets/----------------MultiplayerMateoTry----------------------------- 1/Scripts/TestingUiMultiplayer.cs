using System.Collections; // ✅ necesario para IEnumerator
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

public class RelayManager : MonoBehaviour
{
    [SerializeField] Button hostButton;
    [SerializeField] Button joinButton;
    [SerializeField] InputField joinInput;
    [SerializeField] Text codeText;

    [Header("DB")]
    [SerializeField] private MatchApi matchApi; // arrastra el MatchApi (DB) aquí

    async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        hostButton.onClick.AddListener(CreateRelay);
        joinButton.onClick.AddListener(() => JoinRelay(joinInput.text));
    }

    async void CreateRelay()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        codeText.text = "Code: " + joinCode;

        var relayServerData = new RelayServerData(allocation, "dtls");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        // ✅ asegurarnos de tener MatchApi
        if (matchApi == null) matchApi = FindFirstObjectByType<MatchApi>();

        // ✅ Un solo flujo: crea match -> registra host -> StartHost
        StartCoroutine(HostFlow(joinCode));
    }

    private IEnumerator HostFlow(string joinCode)
    {
        if (matchApi == null)
        {
            Debug.LogWarning("RelayManager: No se encontró MatchApi. Se inicia host sin guardar match en BD.");
            NetworkManager.Singleton.StartHost();
            yield break;
        }

        // 1) Crear match (online + joinCode)
        yield return StartCoroutine(matchApi.CreateMatch("online", joinCode));

        // 2) Registrar host como jugador del match
        if (Session.CurrentMatchId > 0 && Session.UserId > 0)
        {
            yield return StartCoroutine(matchApi.AddPlayerToMatch(Session.CurrentMatchId, Session.UserId, "host"));
        }
        else
        {
            Debug.LogWarning($"RelayManager: No se pudo registrar host. MatchId={Session.CurrentMatchId}, UserId={Session.UserId}");
        }

        // 3) Arrancar Host
        NetworkManager.Singleton.StartHost();
    }

    async void JoinRelay(string joinCode)
    {
        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        var relayServerData = new RelayServerData(joinAllocation, "dtls");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartClient();

        // ✅ Registrar cliente en match_players por joinCode
        if (matchApi == null) matchApi = FindFirstObjectByType<MatchApi>();
        if (matchApi != null && Session.UserId > 0)
        {
            StartCoroutine(matchApi.JoinMatchByCode(joinCode, Session.UserId));
        }
        else
        {
            Debug.LogWarning($"RelayManager: No se pudo registrar client. MatchApi={(matchApi != null)}, UserId={Session.UserId}");
        }
    }
}
