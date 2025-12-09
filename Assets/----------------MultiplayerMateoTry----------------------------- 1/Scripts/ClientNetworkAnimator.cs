using Unity.Netcode.Components;
using UnityEngine;

public class ClientNetworkAnimator : NetworkAnimator
{
    // This tells Netcode that the Owner (the client) is allowed to sync animations,
    // not just the server.
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}