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
    [SerializeField] private MatchApi matchApi; // arrastra el GO DB (MatchApi) aquí

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

        // ✅ Crear match en BD (online + joinCode + host_user_id = Session.UserId)
        if (matchApi == null) matchApi = FindFirstObjectByType<MatchApi>();
        if (matchApi != null)
        {
            StartCoroutine(matchApi.CreateMatch("online", joinCode));
        }
        else
        {
            Debug.LogWarning("RelayManager: No se encontró MatchApi. No se guardará el match en BD.");
        }

        NetworkManager.Singleton.StartHost();
    }

    async void JoinRelay(string joinCode)
    {
        var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        var relayServerData = new RelayServerData(joinAllocation, "dtls");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        NetworkManager.Singleton.StartClient();
    }
}
