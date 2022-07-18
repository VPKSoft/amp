#region License
/*
MIT License

Copyright(c) 2020 Petteri Kautonen

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
using System.Globalization;
using System.IO;
using VPKSoft.Utils;

namespace amp.UtilityClasses.Settings;

/// <summary>
/// The settings class for the software.
/// </summary>
[Obsolete("Moved to XML format settings.")]
public static class SettingsOld
{
    private const string SettingsFileName = "settings.vnml";
    // ReSharper disable once IdentifierTypo
    private static VPKNml _vnml;

    // an old "fossil" from the past..
    private static int _dbUpdateRequiredLevel = -1;

    /// <summary>
    /// Moves the old settings to XML format.
    /// </summary>
    /// <param name="settings">The settings.</param>
    public static void FromOldSettings(Settings settings)
    {
        var file = Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName;

        if (!File.Exists(file))
        {
            return;
        }

        settings.DbUpdateRequiredLevel = DbUpdateRequiredLevel;
        settings.LoadEntireFileSizeLimit = LoadEntireFileSizeLimit;
        settings.StackRandomPercentage = StackRandomPercentage;
        settings.CultureString = Culture.Name;
        settings.Culture = Culture;
        settings.LoadEntireFileSizeLimit = LoadEntireFileSizeLimit;
        settings.StackRandomPercentage = StackRandomPercentage;
        settings.AlbumNaming = AlbumNaming;
        settings.AlbumNamingRenamed = AlbumNamingRenamed;
        settings.BiasedPlayedCount = BiasedPlayedCount;
        settings.BiasedRandom = BiasedRandom;
        settings.BiasedPlayedCountEnabled = BiasedPlayedCountEnabled;
        settings.BiasedRandomizedCountEnabled = BiasedRandomizedCountEnabled;
        settings.BiasedRating = BiasedRating;
        settings.BiasedSkippedCount = BiasedSkippedCount;
        settings.BiasedSkippedCountEnabled = BiasedSkippedCountEnabled;
        settings.Tolerance = Tolerance;

        // ReSharper disable once IdentifierTypo
        VPKSoft.Utils.VPKNml vnml = new VPKSoft.Utils.VPKNml();
        VPKSoft.Utils.Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
        // ReSharper disable once StringLiteralTypo
        vnml.Load(VPKSoft.Utils.Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + "settings.vnml");

        settings.QuietHours = Convert.ToBoolean(vnml["quietHour", "enabled", false]); // this is gotten from the settings

        settings.QuietHoursFrom = vnml["quietHour", "start", "23:00"].ToString();
        settings.QuietHoursTo = vnml["quietHour", "end", "08:00"].ToString();
        settings.QuietHoursPause = Convert.ToBoolean(vnml["quietHour", "pause", true]);
        settings.QuietHoursVolPercentage = (100.0 - Convert.ToDouble(vnml["quietHour", "percentage", 70])) / 100.0;
        settings.LatencyMs = Convert.ToInt32(vnml["latency", "value", 300]);
        settings.AutoCheckUpdates = Convert.ToBoolean(vnml["autoUpdate", "enabled", false]);

        settings.AudioVisualizationStyle = Convert.ToInt32(vnml["visualizeAudio", "type", 0]);
        settings.AudioVisualizationVisualPercentage = Convert.ToInt32(vnml["visualizeAudio", "percentage", 15]);
        settings.AudioVisualizationCombineChannels = Convert.ToBoolean(vnml["visualizeAudio", "combineChannels", false]);
        settings.BalancedBars = Convert.ToBoolean(vnml["visualizeAudio", "balancedBars", false]);
        settings.BarAmount = Convert.ToInt32(vnml["visualizeAudio", "hertzSpan", 92]);
        settings.StackRandomPercentage = Program.Settings.StackRandomPercentage;

        settings.SaveToFile();

        File.Delete(file);
    }

    /// <summary>
    /// Gets or sets a value of to which level the database is updated to.
    /// </summary>
    public static int DbUpdateRequiredLevel
    {
        get
        {
            if (_dbUpdateRequiredLevel != -1)
            {
                return _dbUpdateRequiredLevel;
            }

            _vnml = new VPKNml();
            Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
            _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
            int result = int.Parse(_vnml["DBUpdateLevel", "value", 0.ToString()].ToString());

            if (_dbUpdateRequiredLevel != -1)
            {
                _vnml["DBUpdateLevel", "value"] = result;
                _vnml.Save(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
            }


            _dbUpdateRequiredLevel = result;
            return result;
        }

        set
        {
            _vnml = new VPKNml();
            Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
            _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
            _vnml["DBUpdateLevel", "value"] = value;
            _dbUpdateRequiredLevel = value;
            _vnml.Save(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
        }
    }

    // a field for the LoadEntireFileSizeLimit property..
    private static int? _loadEntireFileSizeLimit;

    /// <summary>
    /// Gets or sets the limit of a file size to be loaded into the memory for smoother playback.
    /// </summary>
    public static int LoadEntireFileSizeLimit
    {
        get
        {
            if (_loadEntireFileSizeLimit != null)
            {
                return (int) _loadEntireFileSizeLimit;
            }

            _vnml = new VPKNml();
            Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
            _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);

            var result = int.Parse(_vnml["PlayBack", "loadToMemoryLimit", "-1"].ToString());


            if (_loadEntireFileSizeLimit == null)
            {
                _vnml["PlayBack", "loadToMemoryLimit"] = result;
                _vnml.Save(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
                _loadEntireFileSizeLimit = result;
            }

            return result;
        }

        set
        {
            _vnml = new VPKNml();
            Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
            _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
            _vnml["PlayBack", "loadToMemoryLimit"] = value;
            _loadEntireFileSizeLimit = value;
            _vnml.Save(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
        }
    }

    /// <summary>
    /// Gets or sets the stack random percentage.
    /// </summary>
    /// <value>The stack random percentage.</value>
    public static int StackRandomPercentage
    {
        get
        {
            _vnml = new VPKNml();
            Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
            _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);

            var result = int.Parse(_vnml["PlayBack", "stackRandomPercentage", "0"].ToString());
            if (result < 0 || result > 100)
            {
                result = 0;
            }

            return result;
        }

        set
        {
            _vnml = new VPKNml();
            Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
            _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
            _vnml["PlayBack", "stackRandomPercentage"] = value;
            _loadEntireFileSizeLimit = value;
            _vnml.Save(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
        }
    }

    // a field for the AlbumNaming property..
    private static string _albumNaming = string.Empty;

    /// <summary>
    /// Gets or sets the album naming instructions.
    /// </summary>
    public static string AlbumNaming
    {
        get
        {
            if (_albumNaming != string.Empty)
            {
                return _albumNaming;
            }

            _vnml = new VPKNml();
            Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
            _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
            // ReSharper disable once StringLiteralTypo
            string result = _vnml["AlbumNaming", "value", "    #ARTIST? - ##ALBUM? - ##TRACKNO?(^) ##TITLE?##QUEUE? [^]##ALTERNATE_QUEUE?[ *=^]#"].ToString();

            if (_albumNaming == string.Empty)
            {
                _vnml["AlbumNaming", "value"] = result;
                _vnml.Save(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
            }

            _albumNaming = result;
            return result;
        }

        set
        {
            _vnml = new VPKNml();
            Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
            _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
            _vnml["AlbumNaming", "value"] = value;
            _albumNaming = value;
            _vnml.Save(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
        }
    }

    // a field for the AlbumNamingRenamed property..
    private static string _albumNamingRenamed = string.Empty;

    /// <summary>
    /// Gets or sets the renamed album naming instructions.
    /// </summary>
    public static string AlbumNamingRenamed
    {
        get
        {
            if (_albumNamingRenamed != string.Empty)
            {
                return _albumNamingRenamed;
            }

            _vnml = new VPKNml();
            Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
            _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
            string result = _vnml["AlbumNamingRenamed", "value", "    #RENAMED?##QUEUE? [^]##ALTERNATE_QUEUE?[ *=^]#"].ToString();

            if (_albumNamingRenamed == string.Empty)
            {
                _vnml["AlbumNamingRenamed", "value"] = result;
                _vnml.Save(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
            }

            _albumNamingRenamed = result;
            return result;
        }

        set
        {
            _vnml = new VPKNml();
            Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
            _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
            _vnml["AlbumNamingRenamed", "value"] = value;
            _albumNamingRenamed = value;
            _vnml.Save(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
        }
    }

    #region BiasedRandomization
    // a field for the BiasedRandom property..
    private static bool? _biasedRandom;

    /// <summary>
    /// Gets or sets a value indicating whether to use biased randomization with randomizing songs.
    /// </summary>
    public static bool BiasedRandom
    {
        get
        {
            if (_biasedRandom == null)
            {
                _vnml = new VPKNml();
                Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
                _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
                _biasedRandom = bool.Parse(_vnml["biasedRandom", "enabled", false.ToString()].ToString());
            }
            return (bool)_biasedRandom;
        }

        set
        {
            _vnml = new VPKNml();
            Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
            _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
            _vnml["biasedRandom", "enabled"] = value;
            _vnml.Save(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
            _biasedRandom = value;
        }
    }

    // a field for the Tolerance property..
    private static double _tolerance = -1;

    /// <summary>
    /// Gets or sets the tolerance for biased randomization.
    /// </summary>
    public static double Tolerance
    {
        get
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_tolerance == -1)
            {
                _vnml = new VPKNml();
                Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
                _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
                _tolerance =
                    double.Parse(_vnml["biasedRandom", "tolerance", (10.0).ToString(CultureInfo.InvariantCulture)].ToString(),
                        CultureInfo.InvariantCulture);
            }
            return _tolerance;
        }

        set
        {
            _tolerance = value;
            _vnml = new VPKNml();
            Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
            _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
            _vnml["biasedRandom", "tolerance"] = _tolerance.ToString(CultureInfo.InvariantCulture);
            _vnml.Save(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
        }
    }

    // a field for the BiasedRating property..
    private static double _biasedRating = -1;

    /// <summary>
    /// Gets or sets the value for randomization with biased rating.
    /// </summary>
    public static double BiasedRating
    {
        get
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_biasedRating == -1)
            {
                _vnml = new VPKNml();
                Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
                _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
                _biasedRating = 
                    double.Parse(_vnml["biasedRandom", "biasedRating", (50.0).ToString(CultureInfo.InvariantCulture)].ToString(), 
                        CultureInfo.InvariantCulture);
            }
            return _biasedRating;
        }

        set
        {
            _biasedRating = value;
            _vnml = new VPKNml();
            Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
            _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
            _vnml["biasedRandom", "biasedRating"] = _biasedRating.ToString(CultureInfo.InvariantCulture);
            _vnml.Save(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the biased rating in randomization is enabled.
    /// </summary>
    public static bool BiasedRatingEnabled
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

    // a field for the BiasedPlayedCount property..
    private static double _biasedPlayedCount = -1;

    /// <summary>
    /// Gets or sets the value for randomization with biased played count.
    /// </summary>
    public static double BiasedPlayedCount
    {
        get
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_biasedPlayedCount == -1)
            {
                _vnml = new VPKNml();
                Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
                _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);

                _biasedPlayedCount =
                    double.Parse(_vnml["biasedRandom", "biasedPlayedCount", (-1.0).ToString(CultureInfo.InvariantCulture)].ToString(),
                        CultureInfo.InvariantCulture);

            }
            return _biasedPlayedCount;
        }

        set
        {
            _biasedPlayedCount = value;
            _vnml = new VPKNml();
            Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
            _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
            _vnml["biasedRandom", "biasedPlayedCount"] = _biasedPlayedCount.ToString(CultureInfo.InvariantCulture);
            _vnml.Save(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the biased played count in randomization is enabled.
    /// </summary>
    public static bool BiasedPlayedCountEnabled
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

    // a field for the BiasedRandomizedCount property..
    private static double _biasedRandomizedCount = -1;

    /// <summary>
    /// Gets or sets the value for randomization with biased randomized count.
    /// </summary>
    public static double BiasedRandomizedCount
    {
        get
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_biasedRandomizedCount == -1)
            {
                _vnml = new VPKNml();
                Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
                _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);

                _biasedRandomizedCount =
                    double.Parse(_vnml["biasedRandom", "biasedPlayedCount", (-1.0).ToString(CultureInfo.InvariantCulture)].ToString(),
                        CultureInfo.InvariantCulture);
            }
            return _biasedRandomizedCount;
        }

        set
        {
            _biasedRandomizedCount = value;
            _vnml = new VPKNml();
            Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
            _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
            _vnml["biasedRandom", "biasedRandomizedCount"] = _biasedRandomizedCount.ToString(CultureInfo.InvariantCulture);
            _vnml.Save(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the biased randomized count in randomization is enabled.
    /// </summary>
    public static bool BiasedRandomizedCountEnabled
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

    // a field for the BiasedSkippedCount property..
    private static double _biasedSkippedCount = -1;

    /// <summary>
    /// Gets or sets the value for randomization with biased skipped count.
    /// </summary>
    public static double BiasedSkippedCount
    {
        get
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_biasedSkippedCount == -1)
            {
                _vnml = new VPKNml();
                Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
                _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);

                _biasedSkippedCount =
                    double.Parse(_vnml["biasedRandom", "biasedSkippedCount", (-1.0).ToString(CultureInfo.InvariantCulture)].ToString(),
                        CultureInfo.InvariantCulture);

            }
            return _biasedSkippedCount;
        }

        set
        {
            _biasedSkippedCount = value;
            _vnml = new VPKNml();
            Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
            _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
            _vnml["biasedRandom", "biasedSkippedCount"] = _biasedSkippedCount.ToString(CultureInfo.InvariantCulture);
            _vnml.Save(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the biased skipped count in randomization is enabled.
    /// </summary>
    public static bool BiasedSkippedCountEnabled
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

    // the current language (Culture) to be used with the software..
    private static CultureInfo _culture;

    /// <summary>
    /// Gets or sets the current language (Culture) to be used with the software's localization.
    /// </summary>
    public static CultureInfo Culture
    {
        get
        {
            if (_culture == null)
            {
                _vnml = new VPKNml();
                Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
                _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
                string culture = _vnml["culture", "value", "en-US"].ToString();

                _vnml["culture", "value"] = culture;
                _vnml.Save(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
                _culture = new CultureInfo(culture);
            }
            return _culture;
        }

        set
        {
            _vnml = new VPKNml();
            Paths.MakeAppSettingsFolder(Misc.AppType.Winforms);
            _vnml.Load(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
            _vnml["culture", "value"] = value.Name;
            _vnml.Save(Paths.GetAppSettingsFolder(Misc.AppType.Winforms) + SettingsFileName);
            _culture = value;
        }
    }
}