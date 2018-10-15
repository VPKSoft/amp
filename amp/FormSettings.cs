﻿#region license
/*
This file is part of amp#, which is licensed
under the terms of the Microsoft Public License (Ms-Pl) license.
See https://opensource.org/licenses/MS-PL for details.

Copyright (c) VPKSoft 2018
*/
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VPKSoft.LangLib;
using VU = VPKSoft.Utils;

namespace amp
{
    public partial class FormSettings : DBLangEngineWinforms
    {
        public FormSettings()
        {
            InitializeComponent();

            DBLangEngine.DBName = "lang.sqlite";
            if (Utils.ShouldLocalize() != null)
            {
                DBLangEngine.InitalizeLanguage("amp.Messages", Utils.ShouldLocalize(), false);
                return; // After localization don't do anything more.
            }
            DBLangEngine.InitalizeLanguage("amp.Messages");
            btAssignRemoteControlURI.Image = VU.SysIcons.GetSystemIconBitmap(VU.SysIcons.SystemIconType.Shield, new Size(16, 16));
        }

        private void nudQuietHourPercentage_ValueChanged(object sender, EventArgs e)
        {
            rbDecreaseVolumeQuietHours.Checked = true;
        }

        private void bOK_Click(object sender, EventArgs e)
        {
            SaveSettings();
            DialogResult = DialogResult.OK;
        }

        public static void SetMainWindowSettings()
        {
            VU.VPKNml vnml = new VU.VPKNml();
            VU.Paths.MakeAppSettingsFolder();
            vnml.Load(VU.Paths.GetAppSettingsFolder() + "settings.vnml");

            MainWindow.QuietHours = Convert.ToBoolean(vnml["quietHour", "enabled", false]); ; // this is gotten from the settings

            MainWindow.QuietHoursFrom = vnml["quietHour", "start", "23:00"].ToString();
            MainWindow.QuietHoursTo = vnml["quietHour", "end", "08:00"].ToString();
            MainWindow.QuietHoursPause = Convert.ToBoolean(vnml["quietHour", "pause", true]);
            MainWindow.QuietHoursVolPercentage = (100.0 - Convert.ToDouble(vnml["quietHour", "percentage", 70])) / 100.0;
            MainWindow.LatencyMS = Convert.ToInt32(vnml["latency", "value", 300]);
            MainWindow.RemoteControlApiWCF = Convert.ToBoolean(vnml["remote", "enabled", false]);
            MainWindow.RemoteControlApiWCFAddress = vnml["remote", "uri", "http://localhost:11316/ampRemote/"].ToString();
        }

        private static DateTime nextQuietTime = DateTime.Now;

        public static KeyValuePair<DateTime, DateTime> CalculateQuietHour(string hourFrom, string hourTo)
        {
            DateTime dt1 = DateTime.ParseExact(Convert.ToString(hourFrom), "HH':'mm", System.Globalization.CultureInfo.InvariantCulture);
            DateTime dt2 = DateTime.ParseExact(Convert.ToString(hourTo), "HH':'mm", System.Globalization.CultureInfo.InvariantCulture);
            dt1 = new DateTime(nextQuietTime.Year, nextQuietTime.Month, nextQuietTime.Day, dt1.Hour, dt1.Minute, 0);
            dt2 = new DateTime(nextQuietTime.Year, nextQuietTime.Month, nextQuietTime.Day, dt2.Hour, dt2.Minute, 0);

            while (DateTime.Now > dt1 && DateTime.Now < dt2)
            {
                nextQuietTime = nextQuietTime.AddDays(1);
                dt1 = new DateTime(nextQuietTime.Year, nextQuietTime.Month, nextQuietTime.Day, dt1.Hour, dt1.Minute, 0);
                dt2 = new DateTime(nextQuietTime.Year, nextQuietTime.Month, nextQuietTime.Day, dt2.Hour, dt2.Minute, 0);
                if (dt1 > dt2) // 23:00 - 06:00: dt1 = 02.02.2018 23:00 --> dt2 = 03.02.2018 06:00
                {
                    dt2 = dt2.AddDays(1);
                }
            }

            return new KeyValuePair<DateTime, DateTime>(dt1, dt2);
        }

        private static DateTime NextDayTest
        {
            get
            {
                return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 1, 0, 0, 1);
            }
        }

        public static bool IsQuietHour()
        {
            if (!MainWindow.QuietHours)
            {
                return false;
            }

            KeyValuePair<DateTime, DateTime> span = CalculateQuietHour(MainWindow.QuietHoursFrom, MainWindow.QuietHoursTo);
            DateTime test = NextDayTest;
            bool retval = (DateTime.Now >= span.Key && DateTime.Now < span.Value);

//            bool retval = (test >= span.Key && test < span.Value);
            return retval;
        }

        private void SaveSettings()
        {
            VU.VPKNml vnml = new VU.VPKNml();
            VU.Paths.MakeAppSettingsFolder();
            vnml["quietHour", "enabled"] = cbQuietHours.Checked;
            vnml["quietHour", "start"] = dtpFrom.Value.ToString("HH':'mm");
            vnml["quietHour", "end"] = dtpTo.Value.ToString("HH':'mm");
            vnml["quietHour", "percentage"] = (int)nudQuietHourPercentage.Value;
            vnml["quietHour", "pause"] = rbPauseQuiet.Checked;
            vnml["remote", "uri"] = tbRemoteControlURI.Text;
            vnml["latency", "value"] = (int)nudLatency.Value;
            vnml["remote", "enabled"] = cbRemoteControlEnabled.Checked;

            vnml.Save(VU.Paths.GetAppSettingsFolder() + "settings.vnml");

            SetMainWindowSettings();
        }

        private void FormSettings_Shown(object sender, EventArgs e)
        {
            VU.VPKNml vnml = new VU.VPKNml();
            VU.Paths.MakeAppSettingsFolder();
            vnml.Load(VU.Paths.GetAppSettingsFolder() + "settings.vnml");

            cbQuietHours.Checked = Convert.ToBoolean(vnml["quietHour", "enabled", false]);

            dtpFrom.Value = DateTime.ParseExact(Convert.ToString(vnml["quietHour", "start", "23:00"]), "HH':'mm", System.Globalization.CultureInfo.InvariantCulture);
            dtpTo.Value = DateTime.ParseExact(Convert.ToString(vnml["quietHour", "end", "08:00"]), "HH':'mm", System.Globalization.CultureInfo.InvariantCulture);
            nudQuietHourPercentage.Value = Convert.ToInt32(vnml["quietHour", "percentage", 70]);
            rbPauseQuiet.Checked = true;
            rbPauseQuiet.Checked = Convert.ToBoolean(vnml["quietHour", "pause", true]);
            rbDecreaseVolumeQuietHours.Checked = !rbPauseQuiet.Checked;
            tbRemoteControlURI.Text = vnml["remote", "uri", "http://localhost:11316/ampRemote/"].ToString();
            nudLatency.Value = Convert.ToInt32(vnml["latency", "value", 300]);
            cbRemoteControlEnabled.Checked = Convert.ToBoolean(vnml["remote", "enabled", false]);

            bool? netshRet = VU.NetSH.IsNetShUrlReserved(lbRemoteControlURIVValue.Text);
            if (netshRet != null)
            {
                btAssignRemoteControlURI.Enabled = netshRet == false;
            }
        }

        private void tbRemoteControllURI_TextChanged(object sender, EventArgs e)
        {
            if (!VU.UriUrlUtils.ValidHttpUrl(tbRemoteControlURI.Text, true))
            {
                tbRemoteControlURI.BackColor = Color.Red;
            }
            else
            {

                tbRemoteControlURI.BackColor = SystemColors.Window;
                lbRemoteControlURIVValue.Text = VU.UriUrlUtils.MakeWildCardUrl(tbRemoteControlURI.Text, true);
            }
        }

        private void btAssignRemoteControlURI_Click(object sender, EventArgs e)
        {
            bool? netshRet = VU.NetSH.IsNetShUrlReserved(lbRemoteControlURIVValue.Text);
            if (netshRet != null && netshRet == false)
            {
                netshRet = VU.NetSH.ReserveNetShUrl(
                    lbRemoteControlURIVValue.Text, 
                    VU.BuiltInWindowsAccountsLocalize.GetUserNameBySID(VU.BuiltInWindowsAccountsLocalize.Everyone));
            }
        }
    }
}
