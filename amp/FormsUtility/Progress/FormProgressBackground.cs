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
using System.ComponentModel;
using System.Windows.Forms;
using VPKSoft.ErrorLogger;
using VPKSoft.LangLib;

namespace amp.FormsUtility.Progress;

/// <summary>
/// A form the report progress to the user.
/// Implements the <see cref="VPKSoft.LangLib.DBLangEngineWinforms" />
/// </summary>
/// <seealso cref="VPKSoft.LangLib.DBLangEngineWinforms" />
public partial class FormProgressBackground : DBLangEngineWinforms
{
    /// <summary>
    /// A field to hold a current instance of the form.
    /// </summary>
    private static FormProgressBackground formProgressBackground;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormProgressBackground"/> class.
    /// </summary>
    public FormProgressBackground()
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
    }

    private static BackgroundWorker backgroundWorker;
    private static string statusText;

    /// <summary>
    /// Displays the form and aligns it to the center of the <paramref name="form"/>. Afterwards the given <see cref="BackgroundWorker"/> is run.
    /// </summary>
    /// <param name="form">The form to which center the <see cref="FormProgressBackground"/> form should be aligned to.</param>
    /// <param name="worker">A background worker to run upon displaying the form.</param>
    /// <param name="staticStatusText">A text to be shown as a static title describing the process.</param>
    /// <param name="statusLabelText">A text to display the progress percentage in the status label of the form. One place holder for the progress percentage must be reserved (i.e. {0}).</param>
    /// <returns>True if the <see cref="BackgroundWorker"/> instance was successfully run; otherwise false.</returns>
    public static bool Execute(Form form, BackgroundWorker worker, string staticStatusText, string statusLabelText)
    {
        if (formProgressBackground != null || worker == null)
        {
            return false;
        }

        statusText = statusLabelText;
        backgroundWorker = worker;
        formProgressBackground = new FormProgressBackground();
        formProgressBackground.Left = form.Left + (form.Width - formProgressBackground.Width) / 2;
        formProgressBackground.Top = form.Top + (form.Height - formProgressBackground.Height) / 2;
        formProgressBackground.lbStatus.Text = string.Format(statusLabelText, 0);
        formProgressBackground.lbLoading.Text = staticStatusText;
        backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
        backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
        var result = formProgressBackground.ShowDialog() == DialogResult.OK;
        backgroundWorker.ProgressChanged -= BackgroundWorker_ProgressChanged;
        backgroundWorker.RunWorkerCompleted -= BackgroundWorker_RunWorkerCompleted;

        using (formProgressBackground)
        {
            formProgressBackground = null;
        }

        using (worker)
        {
            // dispose of the BackgroundWorker class instance.. 
        }

        return result;
    }

    private static void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        try
        {
            formProgressBackground.DialogResult = e.Cancelled ? DialogResult.Cancel : DialogResult.OK;
        }
        catch (Exception ex)
        {
            // log the exception..
            ExceptionLogger.LogError(ex);
        }
    }

    private static void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        try
        {
            formProgressBackground.Invoke(new MethodInvoker(delegate
            {
                formProgressBackground.lbStatus.Text = string.Format(statusText, e.ProgressPercentage);
                formProgressBackground.pbProgress.Value = e.ProgressPercentage;
            }));
        }
        catch (Exception ex)
        {
            // log the exception..
            ExceptionLogger.LogError(ex);
        }
    }


    private void FormProgressBackground_Shown(object sender, EventArgs e)
    {
        backgroundWorker.RunWorkerAsync();
    }

    private void BtCancel_Click(object sender, EventArgs e)
    {
        backgroundWorker.CancelAsync();
    }
}