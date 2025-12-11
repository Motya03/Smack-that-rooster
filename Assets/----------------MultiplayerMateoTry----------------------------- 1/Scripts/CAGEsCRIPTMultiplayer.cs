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

    public void ClickBattleEnd()
    {
        if (IsServer)
            netAnimator.SetTrigger("Back");
    }
}
