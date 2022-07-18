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
using System.Windows.Forms;
using VPKSoft.LangLib;

namespace amp.FormsUtility.Random;

/// <summary>
/// A class to set parameters for biased randomization.
/// Implements the <see cref="VPKSoft.LangLib.DBLangEngineWinforms" />
/// </summary>
/// <seealso cref="VPKSoft.LangLib.DBLangEngineWinforms" />
public partial class FormRandomizePriority : DBLangEngineWinforms
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormRandomizePriority"/> class.
    /// </summary>
    public FormRandomizePriority()
    {
        InitializeComponent();

        // ReSharper disable once StringLiteralTypo
        DBLangEngine.DBName = "lang.sqlite";

        if (Utils.ShouldLocalize() != null)
        {
            DBLangEngine.InitializeLanguage("amp.Messages", Utils.ShouldLocalize(), false);
            return; // After localization don't do anything more.
        }

        DBLangEngine.InitializeLanguage("amp.Messages");

        suspendCheckedChanged = true;
        cbModifiedRandomizationEnabled.Checked = Program.Settings.BiasedRandom;
        SetBiasedRandomValue(tbRating, cbRatingEnabled, Program.Settings.BiasedRating, Program.Settings.BiasedRatingEnabled);
        SetBiasedRandomValue(tbPlayedCount, cbPlayedCountEnabled, Program.Settings.BiasedPlayedCount, Program.Settings.BiasedPlayedCountEnabled);
        SetBiasedRandomValue(tbRandomizedCount, cbRandomizedCountEnabled, Program.Settings.BiasedRandomizedCount, Program.Settings.BiasedRandomizedCountEnabled);
        SetBiasedRandomValue(tbSkippedCount, cbSkippedCountEnabled, Program.Settings.BiasedSkippedCount, Program.Settings.BiasedSkippedCountEnabled);
        tbTolerancePercentage.Value = Program.Settings.Tolerance < 0 ? 10 : (int)Program.Settings.Tolerance * 10;
        suspendCheckedChanged = false;
    }

    /// <summary>
    /// A field indicating whether the <see cref="cbCommon_CheckedChanged"/> event should suspend executing code.
    /// </summary>
    private readonly bool suspendCheckedChanged;

    /// <summary>
    /// Sets the biased random value for GUI controls.
    /// </summary>
    /// <param name="trackBar">The track bar which value to set.</param>
    /// <param name="checkBox">The check box which value to set.</param>
    /// <param name="biasedRating">The biased rating.</param>
    /// <param name="biasedRatingEnabled">if set to <c>true</c> the biased rating is enabled.</param>
    private void SetBiasedRandomValue(TrackBar trackBar, CheckBox checkBox, double biasedRating, bool biasedRatingEnabled)
    {
        trackBar.Value = (biasedRating >= 0) ? (int)biasedRating * 10 : 0;
        checkBox.Checked = biasedRatingEnabled;
    }

    /// <summary>
    /// Gets the biased random value from a given GUI controls.
    /// </summary>
    /// <param name="trackBar">The track bar of which value to use.</param>
    /// <param name="checkBox">The check box of which value to use.</param>
    /// <param name="enabled">if <c>true</c> the biased randomization is enabled.</param>
    /// <returns>A biased randomization value.</returns>
    private double GetBiasedRandomValue(TrackBar trackBar, CheckBox checkBox, out bool enabled)
    {
        enabled = checkBox.Checked;
        if (!checkBox.Checked)
        {
            return -1;
        }

        return (double)trackBar.Value / 10;
    }

    // the user accepted the settings, so save the settings and return from the dialog..
    private void btOK_Click(object sender, EventArgs e)
    {
        Program.Settings.BiasedRandom = cbModifiedRandomizationEnabled.Checked;

        Program.Settings.BiasedRating = GetBiasedRandomValue(tbRating, cbRatingEnabled, out bool enabled);
        Program.Settings.BiasedRatingEnabled = enabled;

        Program.Settings.BiasedPlayedCount = GetBiasedRandomValue(tbPlayedCount, cbPlayedCountEnabled, out enabled);
        Program.Settings.BiasedPlayedCountEnabled = enabled;

        Program.Settings.BiasedRandomizedCount = GetBiasedRandomValue(tbRandomizedCount, cbRandomizedCountEnabled, out enabled);
        Program.Settings.BiasedRandomizedCountEnabled = enabled;

        Program.Settings.BiasedSkippedCount = GetBiasedRandomValue(tbSkippedCount, cbSkippedCountEnabled, out enabled);
        Program.Settings.BiasedSkippedCountEnabled = enabled;

        Program.Settings.Tolerance = (double)tbTolerancePercentage.Value / 10;

        DialogResult = DialogResult.OK;
    }

    // sets the values to defaults..
    private void btDefault_Click(object sender, EventArgs e)
    {
        tbRating.Value = 500;
        tbPlayedCount.Value = 0;
        tbRandomizedCount.Value = 0;
        tbSkippedCount.Value = 0;
        cbRatingEnabled.Checked = true;
        cbPlayedCountEnabled.Checked = false;
        cbRandomizedCountEnabled.Checked = false;
        cbSkippedCountEnabled.Checked = false;
        tbTolerancePercentage.Value = 100;
        cbModifiedRandomizationEnabled.Checked = false;
    }

    // the tolerance changed; indicate the value as text..
    private void tbTolerancePercentage_ValueChanged(object sender, EventArgs e)
    {
        lbTolerancePercentageValue.Text = $@"{tbTolerancePercentage.Value / 10}";
    }

    // a common handler for the four check boxes..
    private void cbCommon_CheckedChanged(object sender, EventArgs e)
    {
        if (suspendCheckedChanged)
        {
            return;
        }

        cbModifiedRandomizationEnabled.Checked =
            cbRatingEnabled.Checked |
            cbPlayedCountEnabled.Checked |
            cbRandomizedCountEnabled.Checked |
            cbSkippedCountEnabled.Checked;
    }
}