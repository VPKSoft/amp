using System;
using System.Globalization;
using System.Reflection;
using NAudio.Wave;
using VPKSoft.Utils;
using VPKSoft.Utils.XmlSettingsMisc;

namespace amp.UtilityClasses.Settings;

/// <summary>
/// Settings for the amp# software.
/// </summary>
public class Settings : XmlSettings
{
    /// <summary>
    /// Gets or sets the name of the setting file.
    /// </summary>
    /// <value>The name of the setting file.</value>
    internal static string SettingFileName { get; set; } = PathHandler.GetSettingsFile(Assembly.GetEntryAssembly(), ".xml",
        Environment.SpecialFolder.LocalApplicationData);

    /// <summary>
    /// Saves the settings to a file specified by the <see cref="SettingFileName"/> property.
    /// </summary>
    public void SaveToFile()
    {
        Save(SettingFileName);
    }

    /// <summary>
    /// Gets or sets a value of to which level the database is updated to.
    /// </summary>
    [IsSetting]
    [Obsolete("New database migration. Though must keep for history's sake.")]
    public int DbUpdateRequiredLevel { get; set; } = 1;

    #region MiscellaneousSettings        
    /// <summary>
    /// Gets or sets a value indicating whether to automatically hide the album image window if no image is present.
    /// </summary>
    /// <value><c>true</c> if to automatically hide the album image window if no image is present; otherwise, <c>false</c>.</value>
    [IsSetting]
    public bool AutoHideAlbumImage { get; set; }

    /// <summary>
    /// Gets or sets the previous album identifier.
    /// </summary>
    /// <value>The previous album identifier.</value>
    [IsSetting]
    public int PreviousAlbum { get; set; } = 1;

    /// <summary>
    /// The latency in milliseconds for the <see cref="WaveOut.DesiredLatency"/>.
    /// </summary>
    [IsSetting]
    public int LatencyMs { get; set; } = 300;

    /// <summary>
    /// A value indicating whether the software should check for updates automatically upon startup.
    /// </summary>
    [IsSetting]
    public bool AutoCheckUpdates { get; set; }

    /// <summary>
    /// Gets or sets the limit of a file size to be loaded into the memory for smoother playback.
    /// </summary>
    [IsSetting]
    public int LoadEntireFileSizeLimit { get; set; } = -1;

    /// <summary>
    /// Gets or sets the stack random percentage.
    /// </summary>
    /// <value>The stack random percentage.</value>
    [IsSetting]
    public int StackRandomPercentage { get; set; }
    #endregion

    #region AlbumNaming
    /// <summary>
    /// Gets or sets the album naming instructions.
    /// </summary>
    [IsSetting]
    public string AlbumNaming { get; set; } =
        // ReSharper disable once StringLiteralTypo
        @"    #ARTIST? - ##ALBUM? - ##TRACKNO?(^) ##TITLE?##QUEUE? [^]##ALTERNATE_QUEUE?[ *=^]#";

    /// <summary>
    /// Gets or sets the renamed album naming instructions.
    /// </summary>
    [IsSetting]
    public string AlbumNamingRenamed { get; set; } = @"    #RENAMED?##QUEUE? [^]##ALTERNATE_QUEUE?[ *=^]#";
    #endregion

    #region BiasedRandomization
    /// <summary>
    /// Gets or sets a value indicating whether to use biased randomization with randomizing songs.
    /// </summary>
    [IsSetting]
    public bool BiasedRandom { get; set; }

    /// <summary>
    /// Gets or sets the tolerance for biased randomization.
    /// </summary>
    [IsSetting]
    public double Tolerance { get; set; } = 10;

    /// <summary>
    /// Gets or sets the value for randomization with biased rating.
    /// </summary>        
    [IsSetting]
    public double BiasedRating { get; set; } = 50;

    /// <summary>
    /// Gets or sets the value for randomization with biased played count.
    /// </summary>
    [IsSetting]
    public double BiasedPlayedCount { get; set; } = -1;

    /// <summary>
    /// Gets or sets a value indicating whether the biased rating in randomization is enabled.
    /// </summary>
    [IsSetting]
    public bool BiasedRatingEnabled
    {
        get => BiasedRating >= 0;

        set
        {
            if (!value)
            {
                BiasedRating = -1;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the biased played count in randomization is enabled.
    /// </summary>
    [IsSetting]
    public bool BiasedPlayedCountEnabled
    {
        get => BiasedPlayedCount >= 0;

        set
        {
            if (!value)
            {
                BiasedPlayedCount = -1;
            }
        }
    }

    /// <summary>
    /// Gets or sets the value for randomization with biased randomized count.
    /// </summary>
    [IsSetting]
    public double BiasedRandomizedCount { get; set; } = -1;

    /// <summary>
    /// Gets or sets a value indicating whether the biased randomized count in randomization is enabled.
    /// </summary>
    [IsSetting]
    public bool BiasedRandomizedCountEnabled
    {
        get => BiasedRandomizedCount >= 0;

        set
        {
            if (!value)
            {
                BiasedRandomizedCount = -1;
            }
        }
    }

    /// <summary>
    /// Gets or sets the value for randomization with biased skipped count.
    /// </summary>
    [IsSetting]
    public double BiasedSkippedCount { get; set; } = -1;

    /// <summary>
    /// Gets or sets a value indicating whether the biased skipped count in randomization is enabled.
    /// </summary>
    [IsSetting]
    public bool BiasedSkippedCountEnabled
    {
        get => BiasedSkippedCount >= 0;

        set
        {
            if (!value)
            {
                BiasedSkippedCount = -1;
            }
        }
    }
    #endregion

    #region Localization
    /// <summary>
    /// Gets or sets the current language (Culture) to be used with the software's localization.
    /// </summary>
    public CultureInfo Culture { get => new CultureInfo(CultureString); set => CultureString = value.Name; }

    // ReSharper disable CommentTypo, Microsoft documentation, no typos..
    /// <summary
    /// >Gets or sets the culture name in the format languagecode2-country/regioncode2.
    /// </summary>
    /// <value>The culture name in the format languagecode2-country/regioncode2. languagecode2 is a lowercase two-letter code derived from ISO 639-1. country/regioncode2 is derived from ISO 3166 and usually consists of two uppercase letters, or a BCP-47 language tag.</value>
    // ReSharper restore CommentTypo
    [IsSetting]
    public string CultureString { get; set; } = "en-US";
    #endregion

    #region QuietHours
    /// <summary>
    /// A flag indicating whether the quiet hours is enabled in the settings.
    /// </summary>
    [IsSetting]
    public bool QuietHours { get; set; }

    /// <summary>
    /// A value indicating the quiet hour starting time if the <see cref="QuietHours"/> is enabled.
    /// </summary>
    [IsSetting]
    public string QuietHoursFrom { get; set; } = "08:00";

    /// <summary>
    /// A value indicating the quiet hour ending time if the <see cref="QuietHours"/> is enabled.
    /// </summary>
    [IsSetting]
    public string QuietHoursTo { get; set; } = "23:00";

    /// <summary>
    /// A value indicating whether to pause the playback at a quiet hour in case if the <see cref="QuietHours"/> is enabled.
    /// </summary>
    [IsSetting]
    public bool QuietHoursPause { get; set; }

    /// <summary>
    /// A value indicating a volume decrease in percentage if the <see cref="QuietHours"/> is enabled.
    /// </summary>
    [IsSetting]
    public double QuietHoursVolPercentage { get; set; } = 0.7;
    #endregion

    #region GUI        
    /// <summary>
    /// Gets or sets a value indicating whether to display the volume and points selection in the GUI.
    /// </summary>
    /// <value><c>true</c> if to display the volume and points selection in the GUI; otherwise, <c>false</c>.</value>
    [IsSetting]
    public bool DisplayVolumeAndPoints { get; set; } = true;

    /// <summary>
    /// Gets or sets the base volume multiplier.
    /// </summary>
    /// <value>The base volume multiplier.</value>
    [IsSetting]
    public float BaseVolumeMultiplier { get; set; } = 50;
    #endregion

    #region AudioVisualization
    /// <summary>
    /// The audio visualization style.
    /// </summary>
    [IsSetting]
    public int AudioVisualizationStyle { get; set; } = 0;

    /// <summary>
    /// The percentage the audio visualization should take from the main form's playlist area.
    /// </summary>
    [IsSetting]
    public int AudioVisualizationVisualPercentage { get; set; } = 15;

    /// <summary>
    /// A value indicating whether the audio visualization should combine the channels into a single view.
    /// </summary>
    [IsSetting]
    public bool AudioVisualizationCombineChannels { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the audio visualization bar graph bars follow minimum and maximum intensity levels of the current song.
    /// </summary>
    [IsSetting]
    public bool BalancedBars { get; set; }

    /// <summary>
    /// Gets or sets the bar amount to display in the audio visualization.
    /// </summary>
    [IsSetting]
    public int BarAmount { get; set; } = 92;
    #endregion

    #region RESTful
    /// <summary>
    /// Gets or sets a value indicating whether RESTful remote control API is enabled.
    /// </summary>
    /// <value><c>true</c> if the RESTful remote control API is enabled; otherwise, <c>false</c>.</value>
    [IsSetting]
    public bool RestApiEnabled { get; set; }

    /// <summary>
    /// Gets or sets the RESTful remote control API port.
    /// </summary>
    /// <value>The RESTful remote control API port.</value>
    [IsSetting(DefaultValue = 11316)]
    public int RestApiPort { get; set; }
    #endregion

    #region Paths        
    /// <summary>
    /// Gets or sets the previous music file open dialog path from the main form.
    /// </summary>
    /// <value>The previous music file open dialog path from the main form.</value>
    [IsSetting]
    public string PreviousOpenMusicFileDialogPath { get; set; }

    /// <summary>
    /// Gets or sets the previous open music folder dialog path.
    /// </summary>
    /// <value>The the previous open music folder dialog path.</value>
    [IsSetting]
    public string PreviousOpenMusicFolderDialogPath { get; set; }
    #endregion
}