﻿#region License
/*
MIT License

Copyright(c) 2022 Petteri Kautonen

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

using System.ComponentModel;
using amp.Playback.Converters;
using amp.Playback.EventArguments;
using amp.Shared.Classes;
using amp.Shared.Enumerations;
using amp.Shared.Interfaces;
using ManagedBass;
using ManagedBass.Flac;
using ManagedBass.Wma;
using Serilog;
using PlaybackState = amp.Playback.Enumerations.PlaybackState;

namespace amp.Playback;

/// <summary>
/// A playback manager for the amp# software.
/// </summary>
/// <typeparam name="TAudioTrack">The type of the <see cref="IAlbumTrack{TAudioTrack,TAlbum}"/> <see cref="IAlbumTrack{TAudioTrack,TAlbum}.AudioTrack"/> member.</typeparam>
/// <typeparam name="TAlbumTrack">The type of the <see cref="IAlbumTrack{TAudioTrack,TAlbum}"/>.</typeparam>
/// <typeparam name="TAlbum">The type of the <see cref="IAlbumTrack{TAudioTrack,TAlbum}"/> <see cref="IAlbumTrack{TAudioTrack,TAlbum}.Album"/> member.</typeparam>
public class PlaybackManager<TAudioTrack, TAlbumTrack, TAlbum> : IDisposable where TAudioTrack : IAudioTrack where TAlbum : IAlbum where TAlbumTrack : IAlbumTrack<TAudioTrack, TAlbum>
{
    private readonly ILogger? logger;

    private volatile int currentStreamHandle;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackManager{TAudioTrack, TAlbumTrack, TAlbum}"/> class.
    /// </summary>
    /// <param name="logger">The logger to log exceptions, etc.</param>
    /// <param name="getNextTrackFunc">A Task&lt;<see cref="Func{TAlbumTrack}"/>&gt;, which is executed when requesting a next track for playback.</param>
    /// <param name="getTrackById">A Task&lt;<see cref="Func{TIdentity,TAlbumTrack}"/>&gt;, which is executed when requesting track data by its reference identifier for playback.</param>
    /// <param name="doEventsCallback">An action which is executed to continue the application message pumping when the playback thread is being disposed of (joined).</param>
    /// <param name="retryBeforeStopCount">A value indicating how many times to try to play another track on error before stopping the playback entirely.</param>
    /// <remarks>The contents of the <paramref name="getNextTrackFunc"/> must be thread safe as it gets called from another thread.</remarks>
    /// <remarks>The contents of the <paramref name="getTrackById"/> must be thread safe as it gets called from another thread.</remarks>
    public PlaybackManager(ILogger? logger, Func<Task<TAlbumTrack?>> getNextTrackFunc, Func<long, Task<TAlbumTrack?>> getTrackById, Action doEventsCallback, int retryBeforeStopCount)
    {
        this.logger = logger;
        Bass.Init();
        this.getNextTrackFunc = getNextTrackFunc;
        this.getTrackById = getTrackById;
        DoEventsCallback = doEventsCallback;
        this.retryBeforeStopCount = retryBeforeStopCount;
    }

    /// <summary>
    /// Plays the specified track.
    /// </summary>
    /// <param name="track">The track.</param>
    /// <param name="skipStateChange">A value indicating whether to ignore the playback state change caused by this call.</param>
    /// <param name="fromHistory">A value indicating whether the track requested to be played was gotten from the history so it doesn't get recorded again.</param>
    public async Task PlayAudioTrack(IAlbumTrack<TAudioTrack, TAlbum> track, bool skipStateChange, bool fromHistory = false)
    {
        CheckManagerRunning();

        DisposeCurrentChannel();

        if (!File.Exists(track.AudioTrack?.FileName))
        {
            // ReSharper disable once ConvertToCompoundAssignment, volatile field
            errorCount = errorCount + 1;

            var args = new PlaybackErrorFileNotFoundEventArgs
            { FileName = track.AudioTrack?.FileName ?? "", PlayAnother = errorCount >= retryBeforeStopCount, AudioTrackId = track.AudioTrackId, };

            PlaybackErrorFileNotFound?.Invoke(this, args);

            if (args.PlayAnother)
            {
                await getNextTrackFunc();
            }

            return;
        }

        if (FileExtensionConvert.FileNameToFileType(track.AudioTrack?.FileName) == MusicFileType.Flac)
        {
            currentStreamHandle = BassFlac.CreateStream(track.AudioTrack?.FileName ??
                                                      throw new InvalidOperationException(
                                                          "The IAlbumTrack.AudioTrack must be not null."));

        }

        if (FileExtensionConvert.FileNameToFileType(track.AudioTrack?.FileName) == MusicFileType.Wma && UtilityOS.IsWindowsOS)
        {
            currentStreamHandle = BassWma.CreateStream(track.AudioTrack?.FileName ??
                                                     throw new InvalidOperationException(
                                                         "The IAlbumTrack.AudioTrack must be not null."));
        }
        else
        {
            currentStreamHandle = Bass.CreateStream(track.AudioTrack?.FileName ??
                                                  throw new InvalidOperationException(
                                                      "The IAlbumTrack.AudioTrack must be not null."));
        }

        if (currentStreamHandle != 0)
        {
            if (!fromHistory)
            {
                playedTrackIds.Add(track.AudioTrackId);
            }

            skipPlaybackStateChange = skipStateChange;

            if (!Bass.ChannelPlay(currentStreamHandle))
            {
                DisposeCurrentChannel();
                playbackFailed = true;
                return;
            }
            PlaybackVolume = track.AudioTrack.PlaybackVolume;
            trackChanged = previousTrackId != track.AudioTrackId;

            if (trackChanged)
            {
                var playbackPercentage = PreviousPosition / PreviousDuration * 100;
                if (playbackPercentage < SkippedEarlyPercentage)
                {
                    TrackSkipped?.Invoke(this, new TrackSkippedEventArgs { AudioTrackId = PreviousTrackId, SkippedAtPercentage = playbackPercentage, });
                }
            }

            PreviousTrackId = track.AudioTrackId;
        }

        // A "silent" error occurred.
        if (currentStreamHandle == 0)
        {
            LastError = Bass.LastError;

            var count = errorCount + 1;
            errorCount = count;

            var args = new PlaybackErrorEventArgs
            { PlayAnother = errorCount >= retryBeforeStopCount, AudioTrackId = track.AudioTrackId, Error = LastError, };

            PlaybackError?.Invoke(this, args);

            if (args.PlayAnother)
            {
                await getNextTrackFunc();
            }
        }
    }

    /// <summary>
    /// Plays the previous track if one is available.
    /// </summary>
    /// <returns><c>true</c> if the previous track playback was started successfully, <c>false</c> otherwise.</returns>
    public async Task<bool> PreviousTrack()
    {
        CheckManagerRunning();
        if (!playedTrackIds.CanUndo)
        {
            return false;
        }

        var id = playedTrackIds.Undo();
        var track = await getTrackById(id);

        if (track != null)
        {
            await PlayAudioTrack(track, true, true);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the manager is running and throws an <see cref="InvalidOperationException"/> if not.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>.
    private void CheckManagerRunning()
    {
        if (ManagerStopped)
        {
            throw new InvalidOperationException("The manager must be running by setting the ManagerStopped = false.");
        }
    }

    /// <summary>
    /// Occurs when the playback position changed.
    /// </summary>
    /// <remarks>The event subscription code must be thread-safe as it gets invoked from another thread.</remarks>
    public event EventHandler<PlaybackPositionChangedArgs>? PlaybackPositionChanged;

    /// <summary>
    /// Occurs when the track changed.
    /// </summary>
    /// <remarks>The event subscription code must be thread-safe as it gets invoked from another thread.</remarks>
    public event EventHandler<TrackChangedArgs>? TrackChanged;

    /// <summary>
    /// Occurs when the playback state changed.
    /// </summary>
    /// <remarks>The event subscription code must be thread-safe as it gets invoked from another thread.</remarks>
    public event EventHandler<PlaybackStateChangedArgs>? PlaybackStateChanged;

    /// <summary>
    /// Occurs when track playback is skipped early.
    /// </summary>
    public event EventHandler<TrackSkippedEventArgs>? TrackSkipped;

    /// <summary>
    /// Occurs when <see cref="ManagedBass"/> fails to play a track.
    /// </summary>
    public event EventHandler<PlaybackErrorEventArgs>? PlaybackError;

    /// <summary>
    /// Occurs when the file name of the track to be played was not found.
    /// </summary>
    public event EventHandler<PlaybackErrorFileNotFoundEventArgs>? PlaybackErrorFileNotFound;

    private volatile bool stopThread = true;
    private volatile PlaybackState previousPlaybackState;
    private readonly object lockObject = new();
    private volatile bool skipPlaybackStateChange;
    private volatile UndoRedoStack<long> playedTrackIds = new();
    private double volume = 1;
    private double masterVolume = 1;
    private volatile bool shuffle = true;
    private volatile bool playbackFailed;
    private volatile int retryBeforeStopCount;
    private volatile int errorCount;

    [EditorBrowsable(EditorBrowsableState.Never)]
    private double previousPosition;
    [EditorBrowsable(EditorBrowsableState.Never)]
    private long previousTrackId;

    private volatile bool trackChanged;

    private Thread? playbackThread;
    private readonly Func<Task<TAlbumTrack?>> getNextTrackFunc;
    private readonly Func<long, Task<TAlbumTrack?>> getTrackById;
    private double previousDuration;
    private double skippedEarlyPercentage = 65;
    private volatile Errors lastError;

    /// <summary>
    /// Gets or sets the count to retry failed playback before stop trying.
    /// </summary>
    /// <value>The count to retry failed playback before stop trying.</value>
    public int RetryBeforeStopCount
    {
        get => retryBeforeStopCount;

        set => retryBeforeStopCount = value;
    }

    /// <summary>
    /// Resets the playback history.
    /// </summary>
    public void ResetPlaybackHistory()
    {
        lock (lockObject)
        {
            playedTrackIds.Reset();
        }
    }

    /// <summary>
    /// Plays the next track.
    /// </summary>
    /// <param name="skipStateChange">A value indicating whether to ignore the playback state change caused by this call.</param>
    public async Task PlayNextTrack(bool skipStateChange)
    {
        if (!playedTrackIds.CanRedo)
        {
            var nextTrack = await getNextTrackFunc();
            if (nextTrack != null)
            {
                await PlayAudioTrack(nextTrack, skipStateChange);
            }
        }
        else
        {
            var track = await getTrackById(playedTrackIds.Redo());
            if (track != null)
            {
                await PlayAudioTrack(track, skipStateChange, true);
            }
        }
    }

    /// <summary>
    /// Gets or sets the master volume for this <see cref="PlaybackManager{TAudioTrack, TAlbumTrack, TAlbum}"/>.
    /// </summary>
    /// <value>The master volume.</value>
    public double MasterVolume
    {
        get
        {
            lock (lockObject)
            {
                return masterVolume;
            }
        }

        set
        {
            lock (lockObject)
            {
                masterVolume = value;
                try
                {
                    Bass.ChannelSetAttribute(currentStreamHandle, ChannelAttribute.Volume, value * masterVolume);
                }
                catch (Exception ex)
                {
                    logger?.Error(ex, "");
                }
            }
        }
    }

    /// <summary>
    /// Gets a value indicating jumping to previous track is possible.
    /// </summary>
    /// <value><c>true</c> if jumping to previous track is possible; otherwise, <c>false</c>.</value>
    public bool CanGoPrevious
    {
        get
        {
            lock (lockObject)
            {
                return playedTrackIds.CanUndo;
            }
        }
    }

    /// <summary>
    /// Gets or sets the last <see cref="ManagedBass"/> library error.
    /// </summary>
    /// <value>The last error <see cref="ManagedBass"/> library error.</value>
    public Errors LastError
    {
        get => lastError;

        set => lastError = value;
    }

    /// <summary>
    /// Gets or sets the playback volume. <c>0</c> is mute, <c>1</c> if full.
    /// </summary>
    /// <value>The playback volume.</value>
    public double PlaybackVolume
    {
        get
        {
            lock (lockObject)
            {
                try
                {
                    return Bass.ChannelGetAttribute(currentStreamHandle, ChannelAttribute.Volume);
                }
                catch (Exception ex)
                {
                    logger?.Error(ex, "");
                    return volume;
                }
            }
        }

        set
        {
            lock (lockObject)
            {
                volume = value;
                try
                {
                    Bass.ChannelSetAttribute(currentStreamHandle, ChannelAttribute.Volume, value * masterVolume);
                }
                catch (Exception ex)
                {
                    logger?.Error(ex, "");
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the skipped early percentage.
    /// E.g. the track is considered to be skipped if playback is changed below specified position percentage.
    /// </summary>
    /// <value>The skipped early percentage.</value>
    public double SkippedEarlyPercentage
    {
        get
        {
            lock (lockObject)
            {
                return skippedEarlyPercentage;
            }
        }

        set
        {
            lock (lockObject)
            {
                skippedEarlyPercentage = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether shuffle mode is enabled for this <see cref="PlaybackManager{TAudioTrack, TAlbumTrack, TAlbum}"/>.
    /// </summary>
    /// <value><c>true</c> if shuffle mode is enabled; otherwise, <c>false</c>.</value>
    public bool Shuffle
    {
        get => shuffle;

        set => shuffle = value;
    }

    /// <summary>
    /// Resumes the playback if paused, otherwise plays the next track.
    /// </summary>
    public async Task PlayOrResume()
    {
        if (previousPlaybackState == PlaybackState.Paused)
        {
            Bass.ChannelPlay(currentStreamHandle);
        }
        else
        {
            await PlayNextTrack(true);
        }
    }

    /// <summary>
    /// Pauses the playback.
    /// </summary>
    public void Pause()
    {
        if (previousPlaybackState == PlaybackState.Playing)
        {
            Bass.ChannelPause(currentStreamHandle);
        }
    }

    /// <summary>
    /// Gets or sets the previous track identifier (Thread safe).
    /// </summary>
    /// <value>The previous track identifier (Thread safe).</value>
    private long PreviousTrackId
    {
        get
        {
            lock (lockObject)
            {
                return previousTrackId;
            }
        }

        set
        {
            lock (lockObject)
            {
                previousTrackId = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the previous playback position (Thread safe).
    /// </summary>
    /// <value>The previous playback position (Thread safe).</value>
    private double PreviousPosition
    {
        get
        {
            lock (lockObject)
            {
                return previousPosition;
            }
        }

        set
        {
            lock (lockObject)
            {
                previousPosition = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the duration of the previous played track (Thread safe).
    /// </summary>
    /// <value>The duration of the previous played track (Thread safe).</value>
    private double PreviousDuration
    {
        get
        {
            lock (lockObject)
            {
                return previousDuration;
            }
        }

        set
        {
            lock (lockObject)
            {
                previousDuration = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the playback position in percentage.
    /// </summary>
    /// <value>The playback position in percentage.</value>
    public double PlaybackPositionPercentage
    {
        get => PlaybackLength == 0 ? 0 : PlaybackPosition / PlaybackLength * 100;

        set => PlaybackPosition = value == 0 ? 0 : PlaybackLength * value / 100;
    }

    /// <summary>
    /// Gets or sets the playback position in seconds.
    /// </summary>
    /// <value>The playback position.</value>
    public double PlaybackPosition
    {
        get
        {
            try
            {
                if (currentStreamHandle != 0)
                {
                    var positionBytes = Bass.ChannelGetPosition(currentStreamHandle);
                    return Bass.ChannelBytes2Seconds(currentStreamHandle, positionBytes);
                }

                return 0;
            }
            catch (Exception ex)
            {
                logger?.Error(ex, "");
                return 0;
            }
        }

        set
        {
            try
            {
                if (currentStreamHandle != 0)
                {
                    var lengthTotalBytes = Bass.ChannelGetLength(currentStreamHandle);
                    var lengthTotal = Bass.ChannelBytes2Seconds(currentStreamHandle, lengthTotalBytes);

                    var positionBytes = (long)(value / lengthTotal * lengthTotalBytes);

                    if (positionBytes > lengthTotalBytes)
                    {
                        positionBytes = lengthTotalBytes;
                    }

                    Bass.ChannelSetPosition(currentStreamHandle, positionBytes);
                }
            }
            catch (Exception ex)
            {
                logger?.Error(ex, "");
            }
        }
    }


    /// <summary>
    /// Gets the length of the playback in seconds.
    /// </summary>
    /// <value>The length of the playback.</value>
    public double PlaybackLength
    {
        get
        {
            try
            {
                if (currentStreamHandle != 0)
                {
                    var positionBytes = Bass.ChannelGetLength(currentStreamHandle);
                    return Bass.ChannelBytes2Seconds(currentStreamHandle, positionBytes);
                }

                return 0;
            }
            catch (Exception ex)
            {
                logger?.Error(ex, "");
                return 0;
            }
        }
    }

    /// <summary>
    /// Gets the state of the playback.
    /// </summary>
    /// <value>The state of the playback.</value>
    public PlaybackState PlaybackState => previousPlaybackState;

    /// <summary>
    /// Gets or sets a value indicating whether this <see cref="PlaybackManager{TAudioTrack, TAlbumTrack, TAlbum}"/> thread is stopped.
    /// </summary>
    /// <value><c>true</c> if this <see cref="PlaybackManager{TAudioTrack, TAlbumTrack, TAlbum}"/> thread is stopped; otherwise, <c>false</c>.</value>
    public bool ManagerStopped
    {
        get => stopThread;

        set
        {
            if (stopThread != value)
            {
                stopThread = value;

                if (stopThread && playbackThread != null)
                {
                    while (!playbackThread.Join(1000))
                    {
                        DoEventsCallback?.Invoke();
                    }

                    FreeBass();
                    playbackThread = null;
                }

                if (!stopThread && playbackThread == null)
                {
                    playbackThread = new Thread(PlaybackThreadMethod);
                    playbackThread.Start();
                }
            }
        }
    }

    /// <summary>
    /// An action which is executed to continue the application message pumping when the playback thread is being disposed of (joined).
    /// </summary>
    public Action DoEventsCallback { get; set; }

    /// <summary>
    /// Disposes the current playback channel if any.
    /// </summary>
    private void DisposeCurrentChannel()
    {
        try
        {
            previousPlaybackState = PlaybackState.Stopped;

            if (currentStreamHandle != 0)
            {
                Bass.ChannelStop(currentStreamHandle);
                Bass.StreamFree(currentStreamHandle);
                currentStreamHandle = 0;
            }
        }
        catch (Exception ex)
        {
            logger?.Error(ex, "");
        }
    }

    /// <summary>
    /// The thread method handling the audio playback.
    /// </summary>
    private async void PlaybackThreadMethod()
    {
        while (!stopThread)
        {
            double position = 0, duration = 0;
            var playbackState = AmpPlaybackStateConverter.ConvertFrom(ManagedBass.PlaybackState.Stopped);

            if (currentStreamHandle != 0)
            {
                var bytes = Bass.ChannelGetLength(currentStreamHandle);
                duration = Bass.ChannelBytes2Seconds(currentStreamHandle, bytes);
                var positionBytes = Bass.ChannelGetPosition(currentStreamHandle);
                position = Bass.ChannelBytes2Seconds(currentStreamHandle, positionBytes);
                playbackState = AmpPlaybackStateConverter.ConvertFrom(Bass.ChannelIsActive(currentStreamHandle));

                if (Math.Abs(PreviousPosition - position) > Globals.FloatingPointTolerance || trackChanged)
                {
                    PlaybackPositionChanged?.Invoke(this,
                        new PlaybackPositionChangedArgs
                        {
                            CurrentPosition = position,
                            PlaybackLength = duration,
                            PlaybackState = playbackState,
                            AudioTrackId = PreviousTrackId,
                        });
                }

                PreviousPosition = position;
                PreviousDuration = duration;
            }

            if (trackChanged)
            {
                trackChanged = false;

                TrackChanged?.Invoke(this,
                    new TrackChangedArgs
                    {
                        CurrentPosition = position,
                        PlaybackLength = duration,
                        PlaybackState = playbackState,
                        AudioTrackId = PreviousTrackId,
                    });
            }

            if (previousPlaybackState != playbackState)
            {
                PlaybackStateChanged?.Invoke(this,
                    new PlaybackStateChangedArgs
                    {
                        CurrentPosition = position,
                        PlaybackLength = duration,
                        PlaybackState = playbackState,
                        AudioTrackId = PreviousTrackId,
                        PreviousPlaybackState = previousPlaybackState,
                    });

                if (playbackFailed)
                {
                    await PlayNextTrack(false);
                }
                else
                {
                    if (!skipPlaybackStateChange)
                    {
                        if (previousPlaybackState == PlaybackState.Playing && playbackState == PlaybackState.Stopped)
                        {
                            await PlayNextTrack(false);
                        }
                    }

                }
                skipPlaybackStateChange = false;
            }

            previousPlaybackState = playbackState;

            Thread.Sleep(100);
        }
    }

    /// <summary>
    /// Releases the resources used by the ManagedBass library.
    /// </summary>
    private void FreeBass()
    {
        try
        {
            DisposeCurrentChannel();
            Bass.Free();
        }
        catch (Exception ex)
        {
            logger?.Error(ex, "");
        }
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        ManagerStopped = true;
        FreeBass();
    }
}