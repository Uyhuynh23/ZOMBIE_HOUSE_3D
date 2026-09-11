using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Persistent server-authoritative match snapshot. Gameplay systems write here
/// only on the server; clients use these values as their UI/rendering truth.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public sealed class NetworkMatchState : NetworkBehaviour
{
    public static NetworkMatchState Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    public readonly NetworkVariable<MatchPhase> Phase = new(
        MatchPhase.Lobby,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public readonly NetworkVariable<FixedString64Bytes> CurrentMap = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public readonly NetworkVariable<int> CurrentRound = new(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public readonly NetworkVariable<int> CurrentWaveIndex = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int> ActiveEnemyCount = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int> RemainingToSpawn = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<bool> AllWavesComplete = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int> HouseHealth = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int> TeamSun = new(50,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int> MatchSeed = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public event Action<MatchPhase, MatchPhase> PhaseChanged;

    private readonly HashSet<ulong> introReadyClients = new();
    private Coroutine pendingWin;

    public override void OnNetworkSpawn()
    {
        Instance = this;
        Phase.OnValueChanged += OnPhaseValueChanged;
        PhaseChanged?.Invoke(Phase.Value, Phase.Value);
    }

    public override void OnNetworkDespawn()
    {
        Phase.OnValueChanged -= OnPhaseValueChanged;
        if (Instance == this) Instance = null;
    }

    public override void OnDestroy()
    {
        if (Instance == this) Instance = null;
        base.OnDestroy();
    }

    private void OnPhaseValueChanged(MatchPhase previous, MatchPhase current)
    {
        Debug.Log($"[NET][MATCH] Phase {previous} -> {current} map={CurrentMap.Value} round={CurrentRound.Value}.");
        PhaseChanged?.Invoke(previous, current);
    }

    public void ServerSetLoading(string mapName, int round)
    {
        if (!IsServer) return;
        introReadyClients.Clear();
        if (pendingWin != null) { StopCoroutine(pendingWin); pendingWin = null; }
        CurrentMap.Value = mapName;
        CurrentRound.Value = Mathf.Max(1, round);
        CurrentWaveIndex.Value = 0;
        ActiveEnemyCount.Value = 0;
        RemainingToSpawn.Value = 0;
        AllWavesComplete.Value = false;
        TeamSun.Value = 50;
        HouseHealth.Value = 0;
        MatchSeed.Value = UnityEngine.Random.Range(1, int.MaxValue);
        Phase.Value = MatchPhase.Loading;
        Debug.Log($"[NET][MATCH] Server set Loading map={mapName} round={CurrentRound.Value}.");
    }

    public void ServerSetIntro()
    {
        if (!IsServer) return;
        introReadyClients.Clear();
        Phase.Value = MatchPhase.Intro;
    }

    public void ReportLocalIntroComplete()
    {
        if (!IsSpawned) return;
        ReportIntroCompleteRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ReportIntroCompleteRpc(RpcParams rpcParams = default)
    {
        if (Phase.Value != MatchPhase.Intro) return;

        introReadyClients.Add(rpcParams.Receive.SenderClientId);
        int connectedCount = NetworkManager != null ? NetworkManager.ConnectedClientsIds.Count : 0;
        Debug.Log($"[NET][MATCH] Intro ready client={rpcParams.Receive.SenderClientId} ready={introReadyClients.Count}/{connectedCount}.");
        if (connectedCount > 0 && introReadyClients.Count >= connectedCount)
            Phase.Value = MatchPhase.Playing;
    }

    public void ServerEndMatch()
    {
        if (IsServer) Phase.Value = MatchPhase.Ended;
    }

    public bool ServerCanMutateGameplay => IsServer && Phase.Value == MatchPhase.Playing;

    public void ServerSetWave(int zeroBasedIndex, int remaining)
    {
        if (!IsServer) return;
        CurrentWaveIndex.Value = Mathf.Max(0, zeroBasedIndex);
        RemainingToSpawn.Value = Mathf.Max(0, remaining);
    }

    public void ServerSetRemainingToSpawn(int value)
    {
        if (IsServer) RemainingToSpawn.Value = Mathf.Max(0, value);
    }

    public void ServerEnemySpawned()
    {
        if (IsServer) ActiveEnemyCount.Value++;
    }

    public void ServerEnemyDied()
    {
        if (IsServer) ActiveEnemyCount.Value = Mathf.Max(0, ActiveEnemyCount.Value - 1);
    }

    public void ServerSetHouseHealth(int value)
    {
        if (IsServer) HouseHealth.Value = Mathf.Max(0, value);
    }

    public bool ServerTrySpendSun(int amount)
    {
        if (!ServerCanMutateGameplay || amount < 0 || TeamSun.Value < amount) return false;
        TeamSun.Value -= amount;
        Debug.Log($"[NET][ECON] Spend amount={amount} balance={TeamSun.Value}.");
        return true;
    }

    public void ServerAddSun(int amount)
    {
        if (!ServerCanMutateGameplay || amount <= 0) return;
        TeamSun.Value += amount;
        Debug.Log($"[NET][ECON] Add amount={amount} balance={TeamSun.Value}.");
    }

    /// <summary>
    /// Win is committed at end-of-frame so a house destruction in the same
    /// simulation window wins arbitration. This makes Lost the documented tie
    /// breaker instead of depending on callback order.
    /// </summary>
    public void ServerRequestWin()
    {
        if (!ServerCanMutateGameplay || pendingWin != null) return;
        pendingWin = StartCoroutine(CommitWinAtEndOfFrame());
    }

    private System.Collections.IEnumerator CommitWinAtEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        pendingWin = null;
        if (ServerCanMutateGameplay) ServerTrySetTerminal(MatchPhase.Won);
    }

    public bool ServerTrySetTerminal(MatchPhase terminal)
    {
        if (!IsServer || (terminal != MatchPhase.Won && terminal != MatchPhase.Lost)) return false;
        if (Phase.Value != MatchPhase.Playing) return false;
        Phase.Value = terminal;
        Debug.Log($"[NET][MATCH] Terminal={terminal} round={CurrentRound.Value} wave={CurrentWaveIndex.Value + 1}.");
        return true;
    }

    public bool ServerBeginTransition()
    {
        if (!IsServer) return false;
        MatchPhase phase = Phase.Value;
        if (phase == MatchPhase.Loading || phase == MatchPhase.Transition) return false;
        Phase.Value = MatchPhase.Transition;
        return true;
    }
}
