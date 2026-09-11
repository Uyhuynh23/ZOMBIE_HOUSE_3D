/// <summary>
/// Server-owned lifecycle for a Phase 1 match. Wave state is deliberately excluded.
/// </summary>
public enum MatchPhase : byte
{
    Lobby = 0,
    Loading = 1,
    Intro = 2,
    Playing = 3,
    Won = 4,
    Lost = 5,
    Transition = 6,
    Ended = 7
}

public enum NetworkPlayMode : byte
{
    None = 0,
    Solo = 1,
    OnlineHost = 2,
    OnlineGuest = 3
}
