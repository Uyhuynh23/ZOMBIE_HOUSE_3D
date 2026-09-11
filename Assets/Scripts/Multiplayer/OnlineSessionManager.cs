using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

/// <summary>
/// Owns the Unity Multiplayer Services session handle. The MPS Sessions SDK
/// configures Relay and starts NGO through WithRelayNetwork.
/// </summary>
[DisallowMultipleComponent]
public sealed class OnlineSessionManager : MonoBehaviour
{
    public const int MaxPlayers = 2;

    public ISession CurrentSession { get; private set; }
    public bool IsBusy { get; private set; }
    public string LastError { get; private set; } = string.Empty;
    public string RoomCode => CurrentSession?.Code ?? string.Empty;
    public int PlayerCount => CurrentSession?.PlayerCount ?? 0;
    public bool IsHost => CurrentSession?.IsHost ?? false;

    public event Action SessionChanged;
    public event Action<string> OperationFailed;
    public event Action SessionEnded;

    private bool servicesReady;
    private string initializationProfile;
    private bool leaving;

    public void SetInitializationProfile(string profile)
    {
        if (servicesReady) throw new InvalidOperationException("Unity Services is already initialized.");
        initializationProfile = profile;
    }

    public async Task InitializeAsync()
    {
        if (servicesReady) return;

        if (string.IsNullOrWhiteSpace(initializationProfile))
            await UnityServices.InitializeAsync();
        else
            await UnityServices.InitializeAsync(new InitializationOptions().SetProfile(initializationProfile));
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        servicesReady = true;
        Debug.Log($"[NET][SESSION] Services ready profile={(string.IsNullOrWhiteSpace(initializationProfile) ? "default" : initializationProfile)} player={AuthenticationService.Instance.PlayerId}.");
    }

    public async Task<bool> CreateSessionAsync()
    {
        if (IsBusy)
        {
            Fail("Another online operation is already in progress.");
            return false;
        }
        IsBusy = true;
        LastError = string.Empty;
        try
        {
            await InitializeAsync();
            var options = new SessionOptions
            {
                MaxPlayers = MaxPlayers,
                IsPrivate = true,
                Name = $"Zombie Garden {DateTime.UtcNow:HHmmss}"
            }.WithRelayNetwork();

            CurrentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            Subscribe(CurrentSession);
            SessionChanged?.Invoke();
            Debug.Log($"[NET][SESSION] Created private session code={RoomCode} roster={PlayerCount}/{MaxPlayers} host={IsHost}.");
            return true;
        }
        catch (Exception exception)
        {
            Fail(ToFriendlyError(exception, false));
            return false;
        }
        finally
        {
            IsBusy = false;
            SessionChanged?.Invoke();
        }
    }

    public async Task<bool> JoinSessionAsync(string roomCode)
    {
        if (IsBusy)
        {
            Fail("Another online operation is already in progress.");
            return false;
        }
        string normalized = (roomCode ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Length < 4)
        {
            Fail("Enter a valid room code.");
            return false;
        }

        IsBusy = true;
        LastError = string.Empty;
        try
        {
            await InitializeAsync();
            CurrentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(
                normalized,
                new JoinSessionOptions());
            Subscribe(CurrentSession);
            SessionChanged?.Invoke();
            Debug.Log($"[NET][SESSION] Joined session code={RoomCode} roster={PlayerCount}/{MaxPlayers} host={IsHost}.");
            return true;
        }
        catch (Exception exception)
        {
            Fail(ToFriendlyError(exception, true));
            return false;
        }
        finally
        {
            IsBusy = false;
            SessionChanged?.Invoke();
        }
    }

    public async Task LeaveAsync()
    {
        if (leaving) return;
        ISession session = CurrentSession;
        if (session == null) return;
        leaving = true;

        Unsubscribe(session);
        CurrentSession = null;
        try
        {
            if (session.IsHost)
            {
                // MVP policy deliberately has no host migration. Deleting the
                // session makes every guest take the explicit session-ended path.
                await session.AsHost().DeleteAsync();
                Debug.Log("[NET][SESSION] Host deleted the online session cleanly.");
            }
            else
            {
                await session.LeaveAsync();
                Debug.Log("[NET][SESSION] Guest left the online session cleanly.");
            }
        }
        catch (Exception exception)
        {
            // The network module may dispose the handle first when the host or
            // transport has already stopped. The desired postcondition is still
            // satisfied because CurrentSession was cleared before awaiting.
            if (exception is ObjectDisposedException)
                Debug.Log("[NET][SESSION] Session was already disposed during leave cleanup.");
            else
                Debug.LogWarning($"[NET][SESSION] Leave failed: {exception.Message}");
        }
        finally
        {
            leaving = false;
            SessionChanged?.Invoke();
        }
    }

    private void Subscribe(ISession session)
    {
        session.Changed += OnSessionChanged;
        session.PlayerJoined += OnPlayerChanged;
        session.PlayerHasLeft += OnPlayerChanged;
        session.RemovedFromSession += OnSessionEnded;
        session.Deleted += OnSessionEnded;
    }

    private void Unsubscribe(ISession session)
    {
        session.Changed -= OnSessionChanged;
        session.PlayerJoined -= OnPlayerChanged;
        session.PlayerHasLeft -= OnPlayerChanged;
        session.RemovedFromSession -= OnSessionEnded;
        session.Deleted -= OnSessionEnded;
    }

    private void OnSessionChanged()
    {
        Debug.Log($"[NET][SESSION] Session changed code={RoomCode} roster={PlayerCount}/{MaxPlayers} host={IsHost}.");
        SessionChanged?.Invoke();
    }

    private void OnPlayerChanged(string playerId)
    {
        Debug.Log($"[NET][SESSION] Roster changed player={playerId} roster={PlayerCount}/{MaxPlayers}.");
        SessionChanged?.Invoke();
    }

    private void OnSessionEnded()
    {
        if (CurrentSession != null) Unsubscribe(CurrentSession);
        CurrentSession = null;
        LastError = "The online session ended.";
        Debug.LogWarning("[NET][SESSION] Online session ended by host/service.");
        SessionChanged?.Invoke();
        SessionEnded?.Invoke();
    }

    private void Fail(string message)
    {
        LastError = message;
        Debug.LogWarning($"[NET][SESSION] {message}");
        OperationFailed?.Invoke(message);
        SessionChanged?.Invoke();
    }

    private static string ToFriendlyError(Exception exception, bool joining)
    {
        string raw = exception.Message.ToLowerInvariant();
        if (raw.Contains("full")) return "That room is full (2/2).";
        if (raw.Contains("not found") || raw.Contains("does not exist") || raw.Contains("404"))
            return "Room not found. Check the code and try again.";
        if (raw.Contains("invalid") || raw.Contains("malformed"))
            return "That room code is invalid.";
        if (raw.Contains("auth") || raw.Contains("unauthorized") || raw.Contains("sign"))
            return "Online sign-in failed. Check the service status and try again.";
        if (raw.Contains("timeout") || raw.Contains("network") || raw.Contains("connection"))
            return "Network connection failed. Check your Internet connection and try again.";
        return joining
            ? "Could not join the room. Check the code and your connection."
            : "Could not create an online room. Check your connection and try again.";
    }
}
