using UnityEngine;
using Unity.Netcode.Components;

[DisallowMultipleComponent]
public class ClientNetwork2 : NetworkTransform
{

    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
