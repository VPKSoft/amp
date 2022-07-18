namespace amp.IpcUtils;

/// <summary>
/// A playback command send via task bar jump list using IPC.
/// </summary>
public enum TaskBarPlaybackCommand
{
    /// <summary>
    /// The command is not set.
    /// </summary>
    None,

    /// <summary>
    /// The next song was requested.
    /// </summary>
    Next,

    /// <summary>
    /// The previous song was requested.
    /// </summary>
    Previous,

    /// <summary>
    /// Playback was requested to continue or pause depending on the current playback state.
    /// </summary>
    PausePlayToggle,
}