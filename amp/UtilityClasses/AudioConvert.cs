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
using NAudio.Flac;
using NAudio.MediaFoundation;
using NAudio.Vorbis;
using NAudio.Wave;
using VPKSoft.ErrorLogger;
using File = TagLib.File;

namespace amp.UtilityClasses;

/// <summary>
/// A class for converting audio into different output formats.
/// </summary>
public class AudioConvert
{
    /// <summary>
    /// Creates a <see cref="NAudio.Wave.WaveStream"/> class instance from a give <paramref name="fileName"/>.
    /// </summary>
    /// <param name="fileName">A file name for a music file to create the <see cref="NAudio.Wave.WaveStream"/> for.</param>
    /// <returns>An instance to the <see cref="NAudio.Wave.WaveStream"/> class if the operation was successful; otherwise null.</returns>
    public static WaveStream GetFileReader(string fileName)
    {
        try
        {
            // determine the file type by it's extension..
            if (Constants.FileIsMp3(fileName))
            {
                return new Mp3FileReader(fileName);
            }

            if (Constants.FileIsOgg(fileName))
            {
                return new VorbisWaveReader(fileName);
            }

            if (Constants.FileIsWav(fileName))
            {
                return new WaveFileReader(fileName);
            }

            if (Constants.FileIsFlac(fileName))
            {
                return new FlacReader(fileName);
            }

            if (Constants.FileIsWma(fileName))
            {
                return new MediaFoundationReader(fileName);
            }

            if (Constants.FileIsAacOrM4A(fileName))
            {
                return new MediaFoundationReader(fileName);
            }

            if (Constants.FileIsAif(fileName))
            {
                return new AiffFileReader(fileName);
            }
        }
        catch (Exception ex)
        {
            // log the exception..
            ExceptionLogger.LogError(ex);
            throw; // rethrow the exception..
        }

        // the format is not supported..
        return null;
    }

    // ReSharper disable once CommentTypo
    /// <summary>
    /// Gets or sets the desired bit rate for audio format conversions. The default is 256 kbps.
    /// </summary>
    /// <value>The desired bit rate.</value>
    public static int DesiredBitRate { get; set; } = 256000;

    /// <summary>
    /// Gets the bit rate of a given audio file.
    /// </summary>
    /// <param name="musicFileName">Name of the audio file.</param>
    /// <returns>A detected bit rate of an audio file if the operation was successful; otherwise <see cref="DesiredBitRate"/> is returned.</returns>
    public static int GetBitRate(string musicFileName)
    {
        // IDisposable so using..
        using (var tagFile = File.Create(musicFileName))
        {
            try
            {
                return tagFile.Properties.AudioBitrate * 1000;
            }
            catch (Exception ex)
            {
                // log the exception..
                ExceptionLogger.LogError(ex);
                return DesiredBitRate;
            }
        }
    }

    private static bool isMediaFoundationApiStarted = false;

    /// <summary>
    /// Gets or sets a value indicating whether the <see cref="T:NAudio.MediaFoundation.MediaFoundationApi"/> API is started.
    /// </summary>
    /// <value><c>true</c> if the <see cref="T:NAudio.MediaFoundation.MediaFoundationApi"/> API is started; otherwise, <c>false</c>.</value>
    public static bool IsMediaFoundationApiStarted
    {
        get => isMediaFoundationApiStarted;

        set
        {
            if (value != isMediaFoundationApiStarted)
            {
                try
                {
                    if (value)
                    {
                        MediaFoundationApi.Startup();
                    }
                    else
                    {
                        MediaFoundationApi.Shutdown();
                    }
                    isMediaFoundationApiStarted = value;
                }
                catch (Exception ex)
                {
                    // log the exception..
                    ExceptionLogger.LogError(ex);
                    isMediaFoundationApiStarted = false;
                }
            }
        }
    }

    /// <summary>
    /// Converts a supported file type to a MP3 file.
    /// </summary>
    /// <param name="fromFile">The file to convert from.</param>
    /// <param name="toFile">The file to convert to.</param>
    /// <param name="detectBitRate">A value indicating whether the source file bit rate should be used.</param>
    /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise.</returns>
    public static bool ToMp3(string fromFile, string toFile, bool detectBitRate)
    {
        if (Constants.FileIsMp3(fromFile))
        {
            return false;
        }

        var bitRate = GetBitRate(fromFile);

        var reader = GetFileReader(fromFile);
        if (reader == null)
        {
            return false;
        }

        using (reader)
        {
            try
            {
                MediaFoundationEncoder.EncodeToMp3(reader, toFile, detectBitRate ? bitRate : DesiredBitRate);
                return true;
            }
            catch (Exception ex)
            {
                // log the exception..
                ExceptionLogger.LogError(ex);
                return false;
            }
        }
    }

    /// <summary>
    /// Converts a supported file type to a AAC file.
    /// </summary>
    /// <param name="fromFile">The file to convert from.</param>
    /// <param name="toFile">The file to convert to.</param>
    /// <param name="detectBitRate">A value indicating whether the source file bit rate should be used.</param>
    /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise.</returns>
    public static bool ToAac(string fromFile, string toFile, bool detectBitRate)
    {
        if (Constants.FileIsAacOrM4A(fromFile))
        {
            return false;
        }

        var reader = GetFileReader(fromFile);
        if (reader == null)
        {
            return false;
        }

        using (reader)
        {
            try
            {
                MediaFoundationEncoder.EncodeToAac(reader, toFile, DesiredBitRate);
                return true;
            }
            catch (Exception ex)
            {
                // log the exception..
                ExceptionLogger.LogError(ex);
                return false;
            }
        }
    }

    /// <summary>
    /// Converts a supported file type to a WMA file.
    /// </summary>
    /// <param name="fromFile">The file to convert from.</param>
    /// <param name="toFile">The file to convert to.</param>
    /// <param name="detectBitRate">A value indicating whether the source file bit rate should be used.</param>
    /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise.</returns>
    public static bool ToWma(string fromFile, string toFile, bool detectBitRate)
    {
        if (Constants.FileIsWma(fromFile))
        {
            return false;
        }

        var reader = GetFileReader(fromFile);
        if (reader == null)
        {
            return false;
        }

        using (reader)
        {
            try
            {
                MediaFoundationEncoder.EncodeToWma(reader, toFile, DesiredBitRate);
                return true;
            }
            catch (Exception ex)
            {
                // log the exception..
                ExceptionLogger.LogError(ex);
                return false;
            }
        }
    }

}