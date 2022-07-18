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

using System.Windows.Forms;
using VPKSoft.LangLib;
using VPKSoft.PosLib;

namespace amp.FormsUtility.Help;

/// <summary>
/// A form displaying a small help for the software.
/// Implements the <see cref="VPKSoft.LangLib.DBLangEngineWinforms" />
/// </summary>
/// <seealso cref="VPKSoft.LangLib.DBLangEngineWinforms" />
public partial class FormHelp : DBLangEngineWinforms
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormHelp"/> class.
    /// </summary>
    public FormHelp()
    {
        // Add this form to be positioned..
        PositionForms.Add(this);

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

    /// <summary>
    /// The form is used as a singleton because of the amount of resources the images take.
    /// </summary>
    private static FormHelp thisSingleton;

    /// <summary>
    /// A flag indicating whether the singleton instance is allowed to be disposed of.
    /// </summary>
    private static bool allowDisposal;

    /// <summary>
    /// Shows the singleton instance of this form.
    /// </summary>
    public static void ShowSingleton()
    {
        if (thisSingleton == null)
        {
            thisSingleton = new FormHelp();
            thisSingleton.Show();
        }
        else
        {
            if (!thisSingleton.Visible)
            {
                thisSingleton.Show();
            }
            thisSingleton.BringToFront();
        }
    }

    // the form is closing..
    private void FormHelp_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (!allowDisposal)
        {
            e.Cancel = true; // prevent the disposing of the form..
            Hide(); // ..so do hide.. :-)
        }
    }

    /// <summary>
    /// Disposes the singleton instance of this form.
    /// </summary>
    public static void DisposeSingleton()
    {
        allowDisposal = true;
        if (thisSingleton != null)
        {
            thisSingleton.Close();
            thisSingleton = null;
        }
    }
}