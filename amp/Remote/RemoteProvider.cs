#region License
/*
MIT License

Copyright(c) 2021 Petteri Kautonen

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Data.SQLite;
using amp.Remote.DataClasses;
using amp.UtilityClasses;

// ReSharper disable IdentifierTypo

// ReSharper disable once CheckNamespace
namespace amp.Remote;

/// <summary>
/// Provides access to the software using RESTful/SOAP API.
/// </summary>
public class RemoteProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteProvider"/> class.
    /// </summary>
    /// <param name="pausedFunction">The paused function.</param>
    /// <param name="pauseAction">The pause action.</param>
    /// <param name="playAction">The play action.</param>
    /// <param name="stoppedFunction">The stopped function.</param>
    /// <param name="playingFunction">The playing function.</param>
    /// <param name="setPositionSecondsAction">The set position seconds action.</param>
    /// <param name="queueAction">The queue action.</param>
    /// <param name="queueIdAction">The queue identifier action.</param>
    /// <param name="refreshLoadQueueStatsAction">The refresh load queue stats action.</param>
    /// <param name="albumChangedFunction">The album changed function.</param>
    /// <param name="albumLoadingFunction">The album loading function.</param>
    /// <param name="albumLoadingAction">The album loading action.</param>
    /// <param name="songsChangedFunction">The songs changed function.</param>
    /// <param name="randomizingFunction">The randomizing function.</param>
    /// <param name="randomizingAction">The randomizing action.</param>
    /// <param name="stackQueueFunction">The stack queue function.</param>
    /// <param name="stackQueueAction">The stack queue action.</param>
    /// <param name="shuffleFunction">The shuffle function.</param>
    /// <param name="shuffleAction">The shuffle action.</param>
    /// <param name="removeSongFromAlbumAction">The remove song from album action.</param>
    /// <param name="setRatingFunction">The set rating function.</param>
    /// <param name="setSongVolumeFunction">The set song volume function.</param>
    /// <param name="setVolumeIdFunction">The set volume identifier function.</param>
    /// <param name="setRatingIdFunction">The set rating identifier function.</param>
    /// <param name="getAlbumsFunction">The get albums function.</param>
    /// <param name="selectAlbumFunction">The select album function.</param>
    /// <param name="canGoPreviousFunction">The can go previous function.</param>
    /// <param name="musicFileAction">The music file action.</param>
    /// <param name="musicFileFunction">The music file function.</param>
    /// <param name="currentAlbumFunction">The current album function.</param>
    /// <param name="getNextSongAction">The get next song action.</param>
    /// <param name="getPrevSongAction">The get previous song action.</param>
    /// <param name="getPlaylistFunction">The get playlist function.</param>
    /// <param name="getSecondsFunction">The get seconds function.</param>
    /// <param name="getSecondsTotalFunction">The get seconds total function.</param>
    /// <param name="getFilteredFunction">The get filtered function.</param>
    /// <param name="showQueueAction">The show queue action.</param>
    /// <param name="scrambleQueueFunction">The scramble queue function.</param>
    /// <param name="scrambleQueueSelectedFunction">The scramble queue selected function.</param>
    /// <param name="refreshPlayListAction">The refresh play list action.</param>
    /// <param name="setProgramVolumeFunction">The set program volume function.</param>
    public RemoteProvider(
        Func<bool> pausedFunction,
        Action pauseAction,
        Action<int> playAction,
        Func<bool> stoppedFunction,
        Func<bool> playingFunction,
        Action<double> setPositionSecondsAction,
        Action<bool, List<AlbumSongRemote>> queueAction,
        Action<bool, List<int>> queueIdAction,
        Action<int, bool> refreshLoadQueueStatsAction,
        Func<bool> albumChangedFunction,
        Func<bool> albumLoadingFunction,
        Action<bool> albumLoadingAction,
        Func<bool> songsChangedFunction,
        Func<bool> randomizingFunction,
        Action<bool> randomizingAction,
        Func<bool> stackQueueFunction,
        Action<bool> stackQueueAction,
        Func<bool> shuffleFunction,
        Action<bool> shuffleAction,
        Action<AlbumSongRemote> removeSongFromAlbumAction,
        Func<int, bool> setRatingFunction,
        Func<float, bool> setSongVolumeFunction,
        Func<List<int>, float, bool> setVolumeIdFunction,
        Func<List<int>, int, bool> setRatingIdFunction,
        Func<List<AlbumRemote>> getAlbumsFunction,
        Func<string, bool> selectAlbumFunction,
        Func<bool> canGoPreviousFunction,
        Action<MusicFile> musicFileAction,
        Func<MusicFile> musicFileFunction,
        Func<string, string> currentAlbumFunction,
        Action getNextSongAction,
        Action getPrevSongAction,
        Func<List<MusicFile>, List<MusicFile>> getPlaylistFunction,
        Func<double> getSecondsFunction,
        Func<double> getSecondsTotalFunction,
        Func<FilterType?, FilterType> getFilteredFunction,
        Action showQueueAction,
        Func<bool> scrambleQueueFunction,
        Func<List<int>, bool> scrambleQueueSelectedFunction,
        Action refreshPlayListAction,
        Func<float?, float> setProgramVolumeFunction)
    {
        PausedFunction = pausedFunction;
        PauseAction = pauseAction;
        PlayAction = playAction;
        StoppedFunction = stoppedFunction;
        PlayingFunction = playingFunction;
        SetPositionSecondsAction = setPositionSecondsAction;
        QueueAction = queueAction;
        QueueIdAction = queueIdAction;
        RefreshLoadQueueStatsAction = refreshLoadQueueStatsAction;
        AlbumChangedFunction = albumChangedFunction;
        AlbumLoadingFunction = albumLoadingFunction;
        AlbumLoadingAction = albumLoadingAction;
        SongsChangedFunction = songsChangedFunction;
        RandomizingFunction = randomizingFunction;
        RandomizingAction = randomizingAction;
        StackQueueFunction = stackQueueFunction;
        StackQueueAction = stackQueueAction;
        ShuffleFunction = shuffleFunction;
        ShuffleAction = shuffleAction;
        RemoveSongFromAlbumAction = removeSongFromAlbumAction;
        SetRatingFunction = setRatingFunction;
        SetSongVolumeFunction = setSongVolumeFunction;
        SetVolumeIdFunction = setVolumeIdFunction;
        SetRatingIdFunction = setRatingIdFunction;
        GetAlbumsFunction = getAlbumsFunction;
        SelectAlbumFunction = selectAlbumFunction;
        CanGoPreviousFunction = canGoPreviousFunction;
        MusicFileAction = musicFileAction;
        CurrentAlbumFunction = currentAlbumFunction;
        MusicFileFunction = musicFileFunction;
        GetNextSongAction = getNextSongAction;
        GetPrevSongAction = getPrevSongAction;
        GetPlaylistFunction = getPlaylistFunction;
        GetSecondsFunction = getSecondsFunction;
        GetSecondsTotalFunction = getSecondsTotalFunction;
        GetFilteredFunction = getFilteredFunction;
        ShowQueueAction = showQueueAction;
        ScrambleQueueFunction = scrambleQueueFunction;
        ScrambleQueueSelectedFunction = scrambleQueueSelectedFunction;
        RefreshPlayListAction = refreshPlayListAction;
        SetProgramVolumeFunction = setProgramVolumeFunction;
    }

    /// <summary>
    /// Gets or sets the paused function.
    /// </summary>
    /// <value>The paused function.</value>
    internal Func<bool> PausedFunction { get; set; }

    /// <summary>
    /// Gets or sets the pause action.
    /// </summary>
    /// <value>The pause action.</value>
    internal Action PauseAction { get; set; }

    /// <summary>
    /// Gets or sets the play action.
    /// </summary>
    /// <value>The play action.</value>
    internal Action<int> PlayAction { get; set; }

    /// <summary>
    /// Gets or sets the stopped function.
    /// </summary>
    /// <value>The stopped function.</value>
    internal Func<bool> StoppedFunction { get; set; }

    /// <summary>
    /// Gets or sets the playing function.
    /// </summary>
    /// <value>The playing function.</value>
    internal Func<bool> PlayingFunction { get; set; }

    /// <summary>
    /// Gets or sets the set position seconds action.
    /// </summary>
    /// <value>The set position seconds action.</value>
    internal Action<double> SetPositionSecondsAction { get; set; }

    /// <summary>
    /// Gets or sets the queue action.
    /// </summary>
    /// <value>The queue action.</value>
    internal Action<bool, List<AlbumSongRemote>> QueueAction { get; set; }

    /// <summary>
    /// Gets or sets the queue identifier action.
    /// </summary>
    /// <value>The queue identifier action.</value>
    internal Action<bool, List<int>> QueueIdAction { get; set; }

    /// <summary>
    /// Gets or sets the refresh load queue stats action.
    /// </summary>
    /// <value>The refresh load queue stats action.</value>
    internal Action<int, bool> RefreshLoadQueueStatsAction { get; set; }

    /// <summary>
    /// Gets or sets the album changed function.
    /// </summary>
    /// <value>The album changed function.</value>
    internal Func<bool> AlbumChangedFunction { get; set; }

    /// <summary>
    /// Gets or sets the album loading function.
    /// </summary>
    /// <value>The album loading function.</value>
    internal Func<bool> AlbumLoadingFunction { get; set; }

    /// <summary>
    /// Gets or sets the album loading action.
    /// </summary>
    /// <value>The album loading action.</value>
    internal Action<bool> AlbumLoadingAction { get; set; }

    /// <summary>
    /// Gets or sets the songs changed function.
    /// </summary>
    /// <value>The songs changed function.</value>
    internal Func<bool> SongsChangedFunction { get; set; }

    /// <summary>
    /// Gets or sets the randomizing function.
    /// </summary>
    /// <value>The randomizing function.</value>
    internal Func<bool> RandomizingFunction { get; set; }

    /// <summary>
    /// Gets or sets the randomizing action.
    /// </summary>
    /// <value>The randomizing action.</value>
    internal Action<bool> RandomizingAction { get; set; }

    /// <summary>
    /// Gets or sets the stack queue function.
    /// </summary>
    /// <value>The stack queue function.</value>
    internal Func<bool> StackQueueFunction { get; set; }

    /// <summary>
    /// Gets or sets the stack queue action.
    /// </summary>
    /// <value>The stack queue action.</value>
    internal Action<bool> StackQueueAction { get; set; }

    /// <summary>
    /// Gets or sets the shuffle function.
    /// </summary>
    /// <value>The shuffle function.</value>
    internal Func<bool> ShuffleFunction { get; set; }

    /// <summary>
    /// Gets or sets the shuffle action.
    /// </summary>
    /// <value>The shuffle action.</value>
    internal Action<bool> ShuffleAction { get; set; }

    /// <summary>
    /// Gets or sets the remove song from album action.
    /// </summary>
    /// <value>The remove song from album action.</value>
    internal Action<AlbumSongRemote> RemoveSongFromAlbumAction { get; set; }

    /// <summary>
    /// Gets or sets the set rating function.
    /// </summary>
    /// <value>The set rating function.</value>
    internal Func<int, bool> SetRatingFunction { get; set; }

    /// <summary>
    /// Gets or sets the set song volume function.
    /// </summary>
    /// <value>The set song volume function.</value>
    internal Func<float, bool> SetSongVolumeFunction { get; set; }

    /// <summary>
    /// Gets or sets the set program volume action.
    /// </summary>
    /// <value>The set program volume action.</value>
    internal Func<float?, float> SetProgramVolumeFunction { get; set; }

    /// <summary>
    /// Gets or sets the set volume identifier function.
    /// </summary>
    /// <value>The set volume identifier function.</value>
    internal Func<List<int>, float, bool> SetVolumeIdFunction { get; set; }

    /// <summary>
    /// Gets or sets the set rating identifier function.
    /// </summary>
    /// <value>The set rating identifier function.</value>
    internal Func<List<int>, int, bool> SetRatingIdFunction { get; set; }

    /// <summary>
    /// Gets or sets the get albums function.
    /// </summary>
    /// <value>The get albums function.</value>
    internal Func<List<AlbumRemote>> GetAlbumsFunction { get; set; }

    /// <summary>
    /// Gets or sets the select album function.
    /// </summary>
    /// <value>The select album function.</value>
    internal Func<string, bool> SelectAlbumFunction { get; set; }

    /// <summary>
    /// Gets or sets the can go previous function.
    /// </summary>
    /// <value>The can go previous function.</value>
    internal Func<bool> CanGoPreviousFunction { get; set; }

    /// <summary>
    /// Gets or sets the music file function.
    /// </summary>
    /// <value>The music file function.</value>
    internal Func<MusicFile> MusicFileFunction { get; set; }

    /// <summary>
    /// Gets or sets the music file action.
    /// </summary>
    /// <value>The music file action.</value>
    internal Action<MusicFile> MusicFileAction { get; set; }

    /// <summary>
    /// Gets or sets the current album function.
    /// </summary>
    /// <value>The current album function.</value>
    internal Func<string, string> CurrentAlbumFunction { get; set; }

    /// <summary>
    /// Gets or sets the get next song action.
    /// </summary>
    /// <value>The get next song action.</value>
    internal Action GetNextSongAction { get; set; }

    /// <summary>
    /// Gets or sets the get previous song action.
    /// </summary>
    /// <value>The get previous song action.</value>
    internal Action GetPrevSongAction { get; set; }

    /// <summary>
    /// Gets or sets the get playlist function.
    /// </summary>
    /// <value>The get playlist function.</value>
    internal Func<List<MusicFile>, List<MusicFile>> GetPlaylistFunction { get; set; }

    /// <summary>
    /// Gets or sets the get seconds function.
    /// </summary>
    /// <value>The get seconds function.</value>
    internal Func<double> GetSecondsFunction { get; set; }

    /// <summary>
    /// Gets or sets the get seconds total function.
    /// </summary>
    /// <value>The get seconds total function.</value>
    internal Func<double> GetSecondsTotalFunction { get; set; }

    /// <summary>
    /// Gets or sets the get filtered function.
    /// </summary>
    /// <value>The get filtered function.</value>
    internal Func<FilterType?, FilterType> GetFilteredFunction { get; set; }

    /// <summary>
    /// Gets or sets the show queue action.
    /// </summary>
    /// <value>The show queue action.</value>
    internal Action ShowQueueAction { get; set; }

    /// <summary>
    /// Gets or sets the scramble queue function.
    /// </summary>
    /// <value>The scramble queue function.</value>
    internal Func<bool> ScrambleQueueFunction { get; set; }

    /// <summary>
    /// Gets or sets the scramble queue selected function.
    /// </summary>
    /// <value>The scramble queue selected function.</value>
    internal Func<List<int>, bool> ScrambleQueueSelectedFunction { get; set; }

    /// <summary>
    /// Gets or sets the refresh play list action.
    /// </summary>
    /// <value>The refresh play list action.</value>
    internal Action RefreshPlayListAction { get; set; }

    /// <summary>
    /// Gets a value whether the playback is paused.
    /// </summary>
    /// <returns><c>true</c> if the playback state is paused; otherwise <c>false</c>.</returns>
    public bool Paused()
    {
        return PausedFunction();
    }

    /// <summary>
    /// Pauses the playback.
    /// </summary>
    public void Pause()
    {
        PauseAction();
    }

    /// <summary>
    /// Refreshes the playlist.
    /// </summary>
    public void RefreshPlaylist()
    {
        RefreshPlayListAction();
    }

    /// <summary>
    /// Gets a value whether the playback is stopped.
    /// </summary>
    /// <returns><c>true</c> if the playback is stopped; otherwise <c>false</c>.</returns>
    public bool Stopped()
    {
        return StoppedFunction();
    }

    /// <summary>
    /// Gets a value whether the playback state is playing.
    /// </summary>
    /// <returns><c>true</c> if the playback is playing; otherwise <c>false</c>.</returns>
    public bool Playing()
    {
        return PlayingFunction();
    }

    /// <summary>
    /// Sets the playback position in seconds.
    /// </summary>
    /// <param name="seconds">The playback position in seconds.</param>
    public void SetPositionSeconds(double seconds)
    {
        SetPositionSecondsAction(seconds);
    }

    /// <summary>
    /// Queue a song.
    /// </summary>
    /// <param name="insert">The remote GUI is in insert into the queue mode.</param>
    /// <param name="queueList">A list of songs which are to be queued from the remote GUI.</param>
    public void Queue(bool insert, List<AlbumSongRemote> queueList)
    {
        QueueAction(insert, queueList);
    }

    /// <summary>
    /// Queue a song.
    /// </summary>
    /// <param name="insert">The remote GUI is in insert into the queue mode.</param>
    /// <param name="songIDs">A list of song IDs which are to be queued from the remote GUI.</param>
    public void Queue(bool insert, List<int> songIDs)
    {
        QueueIdAction(insert, songIDs);
    }

    /// <summary>
    /// Refreshes the main window after loading a queue to it.
    /// </summary>
    /// <param name="queueIndex">The queue index (Database ID number) for the queue to load.</param>
    /// <param name="append">If set to <c>true</c> the queue is appended to the previous queue.</param>
    public void RefreshLoadQueueStats(int queueIndex, bool append)
    {
        RefreshLoadQueueStatsAction(queueIndex, append);
    }

    /// <summary>
    /// Gets a value whether the album has changed.
    /// Note: This is an auto-resetting property; after querying the value the property returns false.
    /// </summary>
    public bool AlbumChanged => AlbumChangedFunction();

    /// <summary>
    /// Gets a value whether an album is currently loading.
    /// </summary>
    public bool AlbumLoading => AlbumLoadingFunction();

    /// <summary>
    /// Gets a value whether songs in the album have changed.
    /// </summary>
    public bool SongsChanged => SongsChangedFunction();

    /// <summary>
    /// Gets or sets a value whether the randomization is enabled in the main form.
    /// </summary>
    public bool Randomizing
    {
        get => RandomizingFunction();

        set => RandomizingAction(value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the stack queue is enabled.
    /// </summary>
    public bool StackQueue
    {
        get => StackQueueFunction();

        set => StackQueueAction(value);
    }

    /// <summary>
    /// Gets or sets a value whether shuffling is enabled in the main form.
    /// </summary>
    public bool Shuffle
    {
        get => ShuffleFunction();

        set => ShuffleAction(value);
    }

    /// <summary>
    /// Removes a song from the current album.
    /// </summary>
    /// <param name="albumSongRemote">A <see cref="AlbumSongRemote"/> class instance to remove from the album.</param>
    public void RemoveSongFromAlbum(AlbumSongRemote albumSongRemote)
    {
        RemoveSongFromAlbumAction(albumSongRemote);
    }

    /// <summary>
    /// Sets the rating for the current song.
    /// </summary>
    /// <param name="rating">The new rating value.</param>
    /// <returns><c>true</c> if there is a song to set a rating for; otherwise <c>false</c>.</returns>
    public bool SetRating(int rating)
    {
        return SetRatingFunction(rating);
    }

    /// <summary>
    /// Sets the volume of the currently playing song.
    /// </summary>
    /// <param name="volume">The new volume value.</param>
    /// <returns><c>true</c> if there is a song to set a volume for; otherwise <c>false</c>.</returns>
    public bool SetSongVolume(float volume)
    {
        return SetSongVolumeFunction(volume);
    }

    /// <summary>
    /// Gets or sets the program volume.
    /// </summary>
    public float ProgramVolume
    {
        get => SetProgramVolumeFunction(null);
        set => SetProgramVolumeFunction(value);
    }

    /// <summary>
    /// Sets a volume for multiple songs.
    /// </summary>
    /// <param name="songIdList">A list of song database ID numbers to set the volume for.</param>
    /// <param name="volume">The new volume value.</param>
    /// <returns><c>true</c> if the volume was set successfully; otherwise <c>false</c>.</returns>
    public bool SetSongVolume(List<int> songIdList, float volume)
    {
        return SetVolumeIdFunction(songIdList, volume);
    }

    /// <summary>
    /// Sets a rating for multiple songs.
    /// </summary>
    /// <param name="songIdList">A list of song database ID numbers to set the rating for.</param>
    /// <param name="rating"></param>
    /// <returns><c>true</c> if the rating was set successfully; otherwise <c>false</c>.</returns>
    public bool SetRating(List<int> songIdList, int rating)
    {
        return SetRatingIdFunction(songIdList, rating);
    }

    /// <summary>
    /// Gets the albums currently in the software database.
    /// </summary>
    /// <returns>A list of <see cref="AlbumRemote"/> class instances containing the album data.</returns>
    public List<AlbumRemote> GetAlbums()
    {
        return GetAlbumsFunction();
    }

    /// <summary>
    /// Selects an album with a given name.
    /// </summary>
    /// <param name="name">The name of the album to select.</param>
    /// <returns><c>true</c> if the album was selected successfully; otherwise <c>false</c>.</returns>
    public bool SelectAlbum(string name)
    {
        return SelectAlbumFunction(name);
    }

    /// <summary>
    /// Gets a value whether it is possible to jump to the previously played song.
    /// </summary>
    public bool CanGoPrevious => CanGoPreviousFunction();

    /// <summary>
    /// The currently playing musing file.
    /// </summary>
    public MusicFile MFile
    {
        get => MusicFileFunction();
        set => MusicFileAction(value);
    }

    /// <summary>
    /// The name of a currently playing album.
    /// </summary>
    public string CurrentAlbum
    {
        get => CurrentAlbumFunction(null);
        set => CurrentAlbumFunction(value);
    }

    /// <summary>
    /// Plays the next song. The next song to be played depends on the queue, random and shuffle states of the program.
    /// </summary>
    public void GetNextSong()
    {
        GetNextSongAction();
    }

    /// <summary>
    /// Plays the previous song.
    /// </summary>
    public void GetPrevSong()
    {
        GetPrevSongAction();
    }

    /// <summary>
    /// The list of entries in the current album.
    /// </summary>
    public List<MusicFile> PlayList
    {
        get => GetPlaylistFunction(null);
        set => GetPlaylistFunction(value);
    }

    /// <summary>
    /// The current playback position in seconds.
    /// </summary>
    public double Seconds => GetSecondsFunction();

    /// <summary>
    /// The current song's length in seconds.
    /// </summary>
    public double SecondsTotal => GetSecondsTotalFunction();

    /// <summary>
    /// Gets or sets the type of the playlist filtering.
    /// </summary>
    public FilterType Filtered { get => GetFilteredFunction(null); set => GetFilteredFunction(value); }

    /// <summary>
    /// Displays the queued songs within the playlist.
    /// </summary>
    public void ShowQueue()
    {
        ShowQueueAction();
    }

    /// <summary>
    /// Scrambles the queue to have new random indices.
    /// </summary>
    /// <returns>True if any songs were affected; otherwise false.</returns>
    public bool ScrambleQueue()
    {
        return ScrambleQueueFunction();
    }

    /// <summary>
    /// Starts a playback. If no song is playing the next song depends on whether the randomization and/or shuffle is on or off.
    /// </summary>
    public void Play()
    {
        PlayAction(-1);
    }

    /// <summary>
    /// Plays a song with a given database ID number or the next song if the given id is -1.
    /// </summary>
    /// <param name="id">The database ID number for the song to play.</param>
    public void Play(int id)
    {
        PlayAction(id);
    }

    /// <summary>
    /// Scrambles the queue between the specified selected songs.
    /// </summary>
    /// <param name="scrambleIdList">A list of selected music file identifiers to scramble.</param>
    /// <returns>True if any songs were affected; otherwise false.</returns>
    public bool ScrambleQueueSelected(List<int> scrambleIdList)
    {
        return ScrambleQueueSelectedFunction(scrambleIdList);
    }

    /// <summary>
    /// Gets the songs in the current album. This can take a while if there are thousands of songs in the album.
    /// </summary>
    /// <param name="queued">If true only the queued songs are returned.</param>
    /// <returns>A list of songs in the current album.</returns>
    public List<AlbumSongRemote> GetAlbumSongs(bool queued = false)
    {
        List<AlbumSongRemote> retList = new List<AlbumSongRemote>();
        foreach (MusicFile mf in PlayList)
        {
            if (mf.QueueIndex == 0 && queued)
            {
                continue; // if only queued songs..
            }

            retList.Add(mf.ToAlbumSongRemote());
        }
        return retList;
    }

    /// <summary>
    /// Gets the queued songs.
    /// </summary>
    /// <returns>A list of queued songs in the current album.</returns>
    public List<AlbumSongRemote> GetQueuedSongs()
    {
        return GetAlbumSongs(true);
    }

    /// <summary>
    /// Gets the current state of the amp# music player.
    /// </summary>
    /// <returns></returns>
    public PlayerStateRemote GetPlayerState()
    {
        MusicFile musicFile = MFile;

        int currentSongId = -1;
        double currentSongPosition = 0, currentSongLength = 0;
        string currentSongName = string.Empty;

        bool albumChanged = AlbumChanged;

        if (musicFile != null)
        {
            currentSongId = musicFile.ID;
            currentSongPosition = Seconds;
            currentSongLength = SecondsTotal;
            currentSongName = musicFile.SongNameNoQueue;
        }

        return new PlayerStateRemote
        {
            Paused = Paused(),
            QueueCount = GetQueuedSongs().Count,
            Random = Randomizing,
            StackQueue = StackQueue,
            Playing = Playing(),
            Filtered = GetFilteredFunction(null),
            Shuffle = Shuffle,
            QueueChangedFromPreviousQuery = MusicFile.QueueChanged,
            CurrentSongId = currentSongId,
            CurrentSongLength = currentSongLength,
            CurrentSongName = currentSongName,
            CurrentSongPosition = currentSongPosition,
            CurrentAlbumName = CurrentAlbum,
            Stopped = Stopped(),
            AlbumContentsChanged = albumChanged,
            AlbumChanged = amp.SQLiteDatabase.Database.AlbumChanged || albumChanged,
            SongsChanged = SongsChanged,
            CanGoPrevious = CanGoPrevious,
            AlbumLoading = AlbumLoading,
            AmpVolume = ProgramVolume,
        };
    }

    /// <summary>
    /// Gets a value if the queue was changed from the previous query.
    /// </summary>
    /// <returns>True if the queue was changed from the previous query, otherwise false.</returns>
    public bool QueueChanged()
    {
        return MusicFile.QueueChanged;
    }

    /// <summary>
    /// Gets a value if the play list of the current album was changed from the previous query.
    /// </summary>
    /// <returns>True if the play list of the current album was changed from the previous query, otherwise false.</returns>
    public bool AlbumPlayListChanged()
    {
        return amp.SQLiteDatabase.Database.AlbumChanged;
    }

    /// <summary>
    /// Gets a list of songs which properties were changed (name, volume, rating).
    /// </summary>
    /// <returns>A list of songs which properties have been changed in the current album.</returns>
    public List<AlbumSongRemote> GetChangedSongs()
    {
        List<AlbumSongRemote> retList = new List<AlbumSongRemote>();
        foreach (MusicFile mf in PlayList)
        {
            if (!mf.SongChanged)
            {
                continue; // if only queued songs..
            }

            mf.SongChanged = false;

            retList.Add(new AlbumSongRemote
            {
                Id = mf.ID,
                Duration = mf.Duration,
                Volume = mf.Volume,
                QueueIndex = mf.QueueIndex,
                Rating = mf.Rating,
                SongName = mf.SongName,
                Album = mf.Album,
                Artist = mf.Artist,
                SongNameNoQueue = mf.SongNameNoQueue,
                OverrideName = mf.OverrideName,
                TagStr = mf.TagString,
                Title = mf.Title,
                Track = mf.Track,
                Year = mf.Year,
                FullFileName = mf.FullFileName
            }); // wow what a construct!
        }
        return retList;
    }

    /// <summary>
    /// Gets a list of saved queues for a given album ID.
    /// </summary>
    /// <param name="albumName">A name for of an album which queue list to get. A String.Empty returns saved queues for all albums.</param>
    /// <returns>A list of QueueEntryRemote class instances for the requested album.</returns>
    public List<SavedQueueRemote> GetQueueList(string albumName)
    {
        SQLiteConnection conn = FormMain.Connection; // there is sill a dependency for the MainWindow..

        var savedQueues = amp.SQLiteDatabase.Database.GetAlbumQueues(albumName, conn);

        var savedQueueRemotes = new List<SavedQueueRemote>();


        foreach (var savedQueue in savedQueues)
        {
            var savedQueueRemote = new SavedQueueRemote
            {
                Id = savedQueue.Id,
                AlbumName = savedQueue.AlbumName,
                CountTotal = savedQueue.CountTotal,
                CreteDate = savedQueue.CreteDate,
                QueueName = savedQueue.QueueName,
                QueueSongs = new List<AlbumSongRemote>(),
            };

            foreach (var queueSong in savedQueue.QueueSongs)
            {
                savedQueueRemote.QueueSongs.Add(queueSong.ToAlbumSongRemote());
            }
            savedQueueRemotes.Add(savedQueueRemote);
        }

        return savedQueueRemotes;
    }
}