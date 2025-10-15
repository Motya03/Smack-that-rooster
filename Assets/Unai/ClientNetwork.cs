using UnityEngine;
using Unity.Netcode.Components;

[DisallowMultipleComponent]
public class ClientNetwork : NetworkTransform
{

    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
