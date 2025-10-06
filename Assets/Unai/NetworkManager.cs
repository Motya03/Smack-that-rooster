using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class NetworkManager : MonoBehaviour
{
    public Button serverBtn;
    public Button hostBtn;
    public Button clientBtn;


    private void Awake()
    {
        serverBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
        });
        hostBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
        });
        clientBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
        });
    }


}
