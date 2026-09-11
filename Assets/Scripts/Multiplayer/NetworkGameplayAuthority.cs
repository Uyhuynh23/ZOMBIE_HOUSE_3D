using Unity.Netcode;

/// <summary>Shared authority predicates; no gameplay state is stored here.</summary>
public static class NetworkGameplayAuthority
{
    public static bool IsNetworked => NetworkBootstrap.IsNetworkSession;
    public static bool IsServer => !IsNetworked || (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer);
    public static bool CanMutate
    {
        get
        {
            if (!IsNetworked) return true;
            return NetworkMatchState.Instance != null && NetworkMatchState.Instance.ServerCanMutateGameplay;
        }
    }
}
