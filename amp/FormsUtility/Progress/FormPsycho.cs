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

namespace amp.FormsUtility.Progress;

/// <summary>
/// A form the report progress to the user.
/// Implements the <see cref="VPKSoft.LangLib.DBLangEngineWinforms" />
/// </summary>
/// <seealso cref="VPKSoft.LangLib.DBLangEngineWinforms" />
public partial class FormPsycho : DBLangEngineWinforms
{
    /// <summary>
    /// A field to hold a current instance of the form.
    /// </summary>
    private static FormPsycho formPsycho;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormPsycho"/> class.
    /// </summary>
    public FormPsycho()
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

    /// <summary>
    /// Displays the form and aligns it to the center of the <paramref name="form"/>.
    /// </summary>
    /// <param name="form">The form .</param>
    public static void Execute(Form form)
    {
        if (formPsycho != null)
        {
            return;
        }
        formPsycho = new FormPsycho();
        formPsycho.Left = form.Left + (form.Width - formPsycho.Width) / 2;
        formPsycho.Top = form.Top + (form.Height - formPsycho.Height) / 2;
        formPsycho.Show();
        formPsycho.Refresh();
    }

    /// <summary>
    /// Sets the status text for the instance of this form.
    /// </summary>
    /// <param name="text">The text to display.</param>
    public static void SetStatusText(string text)
    {
        if (formPsycho != null)
        {
            formPsycho.lbStatus.Text = text;
            formPsycho.lbStatus.Refresh();
        }            
    }

    /// <summary>
    /// Closes an instance of this form a and sets it to null.
    /// </summary>
    public static void UnExecute()
    {
        formPsycho.Close();
        formPsycho = null;
    }
}