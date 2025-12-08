using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HitboxMultiplayer
/// - Este script se pone en el objeto del hitbox (con isTrigger)
/// - El owner del hitbox debe asignar ownerPlayer = PlayerMovMultiplayer (quien ataca)
/// - Cuando colisiona con un PlayerMovMultiplayer distinto, llama al ServerRpc del objetivo para aplicar daño.
/// </summary>
[RequireComponent(typeof(Collider))]
public class HitboxMultiplayer : MonoBehaviour
{
    public PlayerMovMultiplayer ownerPlayer; // asignado al spawnear / Start del player
    public int damage = 1;
    public bool onlyHitOncePerEnable = true;

    private HashSet<ulong> hitTargets = new HashSet<ulong>();

    private void OnEnable()
    {
        hitTargets.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ownerPlayer == null) return;

        // evitar golpear partes del owner (hijos)
        if (other.transform.IsChildOf(ownerPlayer.transform)) return;

        PlayerMovMultiplayer target = other.GetComponentInParent<PlayerMovMultiplayer>();
        if (target == null) return;

        // no golpear a sí mismo
        if (target.OwnerClientId == ownerPlayer.OwnerClientId) return;

        // si ya le dimos a este target en esta activación, salimos
        if (onlyHitOncePerEnable && hitTargets.Contains(target.OwnerClientId)) return;

        hitTargets.Add(target.OwnerClientId);

        // Llamamos al ServerRpc del target para que sea el servidor el que aplique daño
        target.ApplyDamageServerRpc(damage, ownerPlayer.OwnerClientId);
    }
}
