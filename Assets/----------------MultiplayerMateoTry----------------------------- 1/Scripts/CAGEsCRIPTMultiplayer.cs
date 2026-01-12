using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class CageScriptMultiplayer : NetworkBehaviour
{
    public NetworkAnimator netAnimator;

    private void Awake()
    {
        if (netAnimator == null)
            netAnimator = GetComponent<NetworkAnimator>();
    }

    private void Start()
    {
        if (IsServer)
            netAnimator.SetTrigger("Fall");
    }
    [ClientRpc]
    public void ClickBattleEndClientRpc()
    {
        if (IsServer)
            Debug.Log("Mateo");
       // Destroy(this.gameObject);
            netAnimator.SetTrigger("BatEn");
    }
    public void Destroy()
    {
        Destroy(this.gameObject);
    }
}
