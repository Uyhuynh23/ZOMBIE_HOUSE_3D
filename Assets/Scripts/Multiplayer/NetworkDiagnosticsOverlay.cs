using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class NetworkDiagnosticsOverlay : MonoBehaviour
{
    private bool visible;
    private GUIStyle style;
    private float nextSample;
    private float frameMs;
    private int networkObjects;
    private int enemies;
    private int plants;
    private int projectiles;
    private ProfilerRecorder sentBytesRecorder;
    private ProfilerRecorder receivedBytesRecorder;
    private ProfilerRecorder rpcSentBytesRecorder;
    private ProfilerRecorder networkVariableSentBytesRecorder;
    private ProfilerRecorder spawnSentBytesRecorder;
    private long sentBytes;
    private long receivedBytes;
    private long rpcSentBytes;
    private long networkVariableSentBytes;
    private long spawnSentBytes;
    private float trafficStartTime;
    private bool wasListening;

    private void Awake()
    {
        if (!Debug.isDebugBuild) { enabled = false; return; }
        sentBytesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Network, "Total Bytes Sent");
        receivedBytesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Network, "Total Bytes Received");
        rpcSentBytesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Network, "RPC Bytes Sent");
        networkVariableSentBytesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Network, "Network Variable Bytes Sent");
        spawnSentBytesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Network, "Object Spawned Bytes Sent");
    }

    private void OnDestroy()
    {
        sentBytesRecorder.Dispose();
        receivedBytesRecorder.Dispose();
        rpcSentBytesRecorder.Dispose();
        networkVariableSentBytesRecorder.Dispose();
        spawnSentBytesRecorder.Dispose();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame) visible = !visible;
        if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame) LogSnapshot();
        frameMs = Mathf.Lerp(frameMs, Time.unscaledDeltaTime * 1000f, 0.08f);
        SampleTraffic();
        if (Time.unscaledTime >= nextSample) { nextSample = Time.unscaledTime + 5f; Sample(); }
    }

    private void SampleTraffic()
    {
        NetworkManager nm = NetworkManager.Singleton;
        bool listening = nm != null && nm.IsListening;
        if (listening && !wasListening)
        {
            sentBytes = receivedBytes = rpcSentBytes = networkVariableSentBytes = spawnSentBytes = 0;
            trafficStartTime = Time.unscaledTime;
        }
        wasListening = listening;
        if (!listening) return;

        if (sentBytesRecorder.Valid) sentBytes += sentBytesRecorder.LastValue;
        if (receivedBytesRecorder.Valid) receivedBytes += receivedBytesRecorder.LastValue;
        if (rpcSentBytesRecorder.Valid) rpcSentBytes += rpcSentBytesRecorder.LastValue;
        if (networkVariableSentBytesRecorder.Valid) networkVariableSentBytes += networkVariableSentBytesRecorder.LastValue;
        if (spawnSentBytesRecorder.Valid) spawnSentBytes += spawnSentBytesRecorder.LastValue;
    }

    private float TrafficSeconds => Mathf.Max(0.001f, Time.unscaledTime - trafficStartTime);

    private void Sample()
    {
        NetworkManager nm = NetworkManager.Singleton;
        networkObjects = nm != null && nm.SpawnManager != null ? nm.SpawnManager.SpawnedObjects.Count : 0;
        enemies = Object.FindObjectsByType<ZombieHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
        plants = Object.FindObjectsByType<PlantBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
        projectiles = Object.FindObjectsByType<PeaProjectile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
    }

    public void LogSnapshot()
    {
        Sample();
        Debug.Log($"[NET][PERF] scene={SceneManager.GetActiveScene().name} frameMs={frameMs:F2} " +
                  $"gcMB={UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / 1048576f:F2} " +
                  $"networkObjects={networkObjects} enemies={enemies} plants={plants} projectiles={projectiles} rttMs={Rtt()} " +
                  $"sentBytes={sentBytes} receivedBytes={receivedBytes} sentBps={sentBytes / TrafficSeconds:F1} " +
                  $"rpcSentBytes={rpcSentBytes} networkVariableSentBytes={networkVariableSentBytes} spawnSentBytes={spawnSentBytes}.");
    }

    private ulong Rtt()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening) return 0;
        UnityTransport transport = nm.GetComponent<UnityTransport>();
        if (transport == null) return 0;
        ulong peer = nm.IsServer && nm.ConnectedClientsIds.Count > 1 ? nm.ConnectedClientsIds[1] : NetworkManager.ServerClientId;
        return transport.GetCurrentRtt(peer);
    }

    private void OnGUI()
    {
        if (!visible) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 15 };
            style.normal.textColor = Color.white;
        }
        NetworkManager nm = NetworkManager.Singleton;
        NetworkMatchState ms = NetworkMatchState.Instance;
        NetworkBootstrap boot = NetworkBootstrap.Instance;
        StringBuilder value = new StringBuilder(384);
        value.AppendLine("NETWORK DEBUG  [F8 toggle / F9 log]");
        value.AppendLine($"Role: {(nm == null || !nm.IsListening ? "Offline" : nm.IsHost ? "Host" : "Client")}  ClientId: {(nm != null ? nm.LocalClientId : 0)}");
        value.AppendLine($"Room: {boot?.OnlineSession?.RoomCode ?? "-"}  Connection: {boot?.LastConnectionStatus ?? "Offline"}");
        value.AppendLine($"Scene: {SceneManager.GetActiveScene().name}  Phase: {(ms != null ? ms.Phase.Value.ToString() : "-")}");
        value.AppendLine($"Round: {(ms != null ? ms.CurrentRound.Value : 0)}  Wave: {(ms != null ? ms.CurrentWaveIndex.Value + 1 : 0)}");
        value.AppendLine($"Enemies: {(ms != null ? ms.ActiveEnemyCount.Value : enemies)}  Incoming: {(ms != null ? ms.RemainingToSpawn.Value : 0)}");
        value.AppendLine($"House: {(ms != null ? ms.HouseHealth.Value : 0)}  Sun: {(ms != null ? ms.TeamSun.Value : 0)}");
        value.AppendLine($"Objects: {networkObjects}  Projectiles: {projectiles}  RTT: {Rtt()} ms");
        value.AppendLine($"Traffic: {sentBytes / TrafficSeconds:F0} B/s up  {receivedBytes / TrafficSeconds:F0} B/s down");
        value.AppendLine($"Sent split: RPC {rpcSentBytes} B  NV {networkVariableSentBytes} B  Spawn {spawnSentBytes} B");
        value.AppendLine($"Frame: {frameMs:F2} ms  Managed: {UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / 1048576f:F2} MB");
        GUI.Box(new Rect(12, 12, 560, 295), value.ToString(), style);
    }
}
