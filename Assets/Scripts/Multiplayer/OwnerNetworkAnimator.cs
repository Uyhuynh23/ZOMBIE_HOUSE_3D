using Unity.Netcode.Components;

/// <summary>Relays animation parameters from the owning player to its peer.</summary>
public sealed class OwnerNetworkAnimator : NetworkAnimator
{
    protected override bool OnIsServerAuthoritative() => false;
}
