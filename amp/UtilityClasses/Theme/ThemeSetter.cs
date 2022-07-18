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

using System.Drawing;
using System.Windows.Forms;
using ReaLTaiizor.Forms;

namespace amp.UtilityClasses.Theme;

internal class ThemeSetter
{
    private void SetTheme(CrownForm form)
    {

    }

    internal static void ColorControls(Color foreColor, Color backColor, params Control[] controls)
    {
        foreach (var label in controls)
        {
            label.ForeColor = foreColor;
            label.BackColor = backColor;
        }
    }

    internal static void FixMenuTheme(MenuStrip menuStrip)
    {
        menuStrip.BackColor = Color.Transparent;
        foreach (ToolStripItem item in menuStrip.Items)
        {
            if (item.GetType().IsAssignableFrom(typeof(ToolStripMenuItem)))
            {
                FixMenuTheme(item as ToolStripMenuItem);
            }
            item.BackColor = Color.Transparent;
        }
    }

    internal static void FixMenuTheme(ToolStripMenuItem menuStrip)
    {
        menuStrip.BackColor = Color.Transparent;
        foreach (ToolStripItem item in menuStrip.DropDownItems)
        {
            if (item.GetType().IsAssignableFrom(typeof(ToolStripMenuItem)))
            {
                FixMenuTheme(item as ToolStripMenuItem);
            }
            item.BackColor = Color.Transparent;
        }
    }
}