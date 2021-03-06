﻿using System.ComponentModel;
using System.Windows.Forms;

namespace amp.FormsUtility.QueueHandling
{
    partial class FormSavedQueues
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSavedQueues));
            this.lvQueues = new System.Windows.Forms.ListView();
            this.colQueueName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colQueueSaveTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tlpMain = new System.Windows.Forms.TableLayoutPanel();
            this.tsMain = new System.Windows.Forms.ToolStrip();
            this.tsbRemove = new System.Windows.Forms.ToolStripButton();
            this.tsbSave = new System.Windows.Forms.ToolStripButton();
            this.tsbModifySavedQueue = new System.Windows.Forms.ToolStripButton();
            this.tsbRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsbExportQueue = new System.Windows.Forms.ToolStripButton();
            this.tsbImportQueue = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbCopyAllFlat = new System.Windows.Forms.ToolStripButton();
            this.bCancel = new System.Windows.Forms.Button();
            this.bOK = new System.Windows.Forms.Button();
            this.sdExportQueue = new System.Windows.Forms.SaveFileDialog();
            this.odExportQueue = new System.Windows.Forms.OpenFileDialog();
            this.btAppendQueue = new System.Windows.Forms.Button();
            this.fbdDirectory = new Ookii.Dialogs.WinForms.VistaFolderBrowserDialog();
            this.scQueue = new System.Windows.Forms.SplitContainer();
            this.lbSongs = new System.Windows.Forms.ListBox();
            this.tlpMain.SuspendLayout();
            this.tsMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scQueue)).BeginInit();
            this.scQueue.Panel1.SuspendLayout();
            this.scQueue.Panel2.SuspendLayout();
            this.scQueue.SuspendLayout();
            this.SuspendLayout();
            // 
            // lvQueues
            // 
            this.lvQueues.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colQueueName,
            this.colQueueSaveTime});
            this.lvQueues.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvQueues.FullRowSelect = true;
            this.lvQueues.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lvQueues.HideSelection = false;
            this.lvQueues.LabelEdit = true;
            this.lvQueues.Location = new System.Drawing.Point(0, 0);
            this.lvQueues.MultiSelect = false;
            this.lvQueues.Name = "lvQueues";
            this.lvQueues.Size = new System.Drawing.Size(523, 272);
            this.lvQueues.TabIndex = 5;
            this.lvQueues.UseCompatibleStateImageBehavior = false;
            this.lvQueues.View = System.Windows.Forms.View.Details;
            this.lvQueues.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.lvQueues_AfterLabelEdit);
            this.lvQueues.BeforeLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.lvQueues_BeforeLabelEdit);
            this.lvQueues.SelectedIndexChanged += new System.EventHandler(this.lvQueues_SelectedIndexChanged);
            // 
            // colQueueName
            // 
            this.colQueueName.Text = "Queue snapshot name";
            this.colQueueName.Width = 352;
            // 
            // colQueueSaveTime
            // 
            this.colQueueSaveTime.Text = "Queue save time";
            this.colQueueSaveTime.Width = 137;
            // 
            // tlpMain
            // 
            this.tlpMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tlpMain.ColumnCount = 2;
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpMain.Controls.Add(this.tsMain, 1, 0);
            this.tlpMain.Controls.Add(this.scQueue, 0, 0);
            this.tlpMain.Location = new System.Drawing.Point(12, 12);
            this.tlpMain.Name = "tlpMain";
            this.tlpMain.RowCount = 1;
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tlpMain.Size = new System.Drawing.Size(553, 417);
            this.tlpMain.TabIndex = 2;
            // 
            // tsMain
            // 
            this.tsMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbRemove,
            this.tsbSave,
            this.tsbModifySavedQueue,
            this.tsbRefresh,
            this.tsbExportQueue,
            this.tsbImportQueue,
            this.toolStripSeparator1,
            this.tsbCopyAllFlat});
            this.tsMain.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.VerticalStackWithOverflow;
            this.tsMain.Location = new System.Drawing.Point(529, 0);
            this.tsMain.Name = "tsMain";
            this.tsMain.Size = new System.Drawing.Size(24, 178);
            this.tsMain.TabIndex = 6;
            this.tsMain.Text = "tsQueueManage";
            // 
            // tsbRemove
            // 
            this.tsbRemove.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbRemove.Enabled = false;
            this.tsbRemove.Image = global::amp.Properties.Resources.Delete;
            this.tsbRemove.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbRemove.Name = "tsbRemove";
            this.tsbRemove.Size = new System.Drawing.Size(22, 20);
            this.tsbRemove.Text = "Remove saved queue";
            this.tsbRemove.ToolTipText = "Remove selected queue snapshot";
            this.tsbRemove.Click += new System.EventHandler(this.tsbRemove_Click);
            // 
            // tsbSave
            // 
            this.tsbSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbSave.Image = global::amp.Properties.Resources.Save;
            this.tsbSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbSave.Name = "tsbSave";
            this.tsbSave.Size = new System.Drawing.Size(22, 20);
            this.tsbSave.Text = "Save changes";
            this.tsbSave.Click += new System.EventHandler(this.tsbSave_Click);
            // 
            // tsbModifySavedQueue
            // 
            this.tsbModifySavedQueue.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbModifySavedQueue.Image = global::amp.Properties.Resources.Modify;
            this.tsbModifySavedQueue.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbModifySavedQueue.Name = "tsbModifySavedQueue";
            this.tsbModifySavedQueue.Size = new System.Drawing.Size(22, 20);
            this.tsbModifySavedQueue.Text = "Modify saved queue";
            this.tsbModifySavedQueue.Click += new System.EventHandler(this.tsbModifySavedQueue_Click);
            // 
            // tsbRefresh
            // 
            this.tsbRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbRefresh.Image = global::amp.Properties.Resources.Refresh;
            this.tsbRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbRefresh.Name = "tsbRefresh";
            this.tsbRefresh.Size = new System.Drawing.Size(22, 20);
            this.tsbRefresh.Text = "Refresh the list";
            this.tsbRefresh.Click += new System.EventHandler(this.tsbRefresh_Click);
            // 
            // tsbExportQueue
            // 
            this.tsbExportQueue.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbExportQueue.Image = global::amp.Properties.Resources.Download;
            this.tsbExportQueue.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbExportQueue.Name = "tsbExportQueue";
            this.tsbExportQueue.Size = new System.Drawing.Size(22, 20);
            this.tsbExportQueue.Text = "Export selected queue";
            this.tsbExportQueue.Click += new System.EventHandler(this.tsbExportQueue_Click);
            // 
            // tsbImportQueue
            // 
            this.tsbImportQueue.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbImportQueue.Image = global::amp.Properties.Resources.Upload;
            this.tsbImportQueue.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbImportQueue.Name = "tsbImportQueue";
            this.tsbImportQueue.Size = new System.Drawing.Size(22, 20);
            this.tsbImportQueue.Text = "Import queue from file";
            this.tsbImportQueue.Click += new System.EventHandler(this.tsbImportQueue_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(22, 6);
            // 
            // tsbCopyAllFlat
            // 
            this.tsbCopyAllFlat.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbCopyAllFlat.Enabled = false;
            this.tsbCopyAllFlat.Image = global::amp.Properties.Resources.copy_all;
            this.tsbCopyAllFlat.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbCopyAllFlat.Name = "tsbCopyAllFlat";
            this.tsbCopyAllFlat.Size = new System.Drawing.Size(22, 20);
            this.tsbCopyAllFlat.Text = "Copy songs into a single directory";
            this.tsbCopyAllFlat.Click += new System.EventHandler(this.TsbCopyAllFlat_Click);
            // 
            // bCancel
            // 
            this.bCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bCancel.Location = new System.Drawing.Point(12, 435);
            this.bCancel.Name = "bCancel";
            this.bCancel.Size = new System.Drawing.Size(75, 23);
            this.bCancel.TabIndex = 11;
            this.bCancel.Text = "Cancel";
            this.bCancel.UseVisualStyleBackColor = true;
            // 
            // bOK
            // 
            this.bOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.bOK.Enabled = false;
            this.bOK.Location = new System.Drawing.Point(490, 435);
            this.bOK.Name = "bOK";
            this.bOK.Size = new System.Drawing.Size(75, 23);
            this.bOK.TabIndex = 10;
            this.bOK.Text = "OK";
            this.bOK.UseVisualStyleBackColor = true;
            // 
            // sdExportQueue
            // 
            this.sdExportQueue.DefaultExt = "*.amp#_qex";
            this.sdExportQueue.Filter = "amp# queue export files (*.amp#_qex)|*.amp#_qex";
            this.sdExportQueue.Title = "Export queue to file";
            // 
            // odExportQueue
            // 
            this.odExportQueue.DefaultExt = "*.amp#_qex";
            this.odExportQueue.Filter = "amp# queue export files (*.amp#_qex)|*.amp#_qex";
            this.odExportQueue.Title = "Import queue from file";
            // 
            // btAppendQueue
            // 
            this.btAppendQueue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btAppendQueue.Location = new System.Drawing.Point(352, 435);
            this.btAppendQueue.Name = "btAppendQueue";
            this.btAppendQueue.Size = new System.Drawing.Size(132, 23);
            this.btAppendQueue.TabIndex = 12;
            this.btAppendQueue.Text = "Append to queue";
            this.btAppendQueue.UseVisualStyleBackColor = true;
            this.btAppendQueue.Click += new System.EventHandler(this.btAppendQueue_Click);
            // 
            // fbdDirectory
            // 
            this.fbdDirectory.Description = "Copy songs into a single directory";
            this.fbdDirectory.UseDescriptionForTitle = true;
            // 
            // scQueue
            // 
            this.scQueue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scQueue.Location = new System.Drawing.Point(3, 3);
            this.scQueue.Name = "scQueue";
            this.scQueue.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // scQueue.Panel1
            // 
            this.scQueue.Panel1.Controls.Add(this.lvQueues);
            // 
            // scQueue.Panel2
            // 
            this.scQueue.Panel2.Controls.Add(this.lbSongs);
            this.scQueue.Size = new System.Drawing.Size(523, 411);
            this.scQueue.SplitterDistance = 272;
            this.scQueue.TabIndex = 7;
            // 
            // lbSongs
            // 
            this.lbSongs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbSongs.FormattingEnabled = true;
            this.lbSongs.Location = new System.Drawing.Point(0, 0);
            this.lbSongs.Name = "lbSongs";
            this.lbSongs.Size = new System.Drawing.Size(523, 135);
            this.lbSongs.TabIndex = 0;
            // 
            // FormSavedQueues
            // 
            this.AcceptButton = this.bOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bCancel;
            this.ClientSize = new System.Drawing.Size(577, 470);
            this.Controls.Add(this.btAppendQueue);
            this.Controls.Add(this.bCancel);
            this.Controls.Add(this.bOK);
            this.Controls.Add(this.tlpMain);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormSavedQueues";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Saved queues";
            this.tlpMain.ResumeLayout(false);
            this.tlpMain.PerformLayout();
            this.tsMain.ResumeLayout(false);
            this.tsMain.PerformLayout();
            this.scQueue.Panel1.ResumeLayout(false);
            this.scQueue.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.scQueue)).EndInit();
            this.scQueue.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private ListView lvQueues;
        private ColumnHeader colQueueName;
        private ColumnHeader colQueueSaveTime;
        private TableLayoutPanel tlpMain;
        private Button bCancel;
        private Button bOK;
        private ToolStrip tsMain;
        private ToolStripButton tsbRemove;
        private ToolStripButton tsbSave;
        private ToolStripButton tsbModifySavedQueue;
        private ToolStripButton tsbRefresh;
        private ToolStripButton tsbExportQueue;
        private ToolStripButton tsbImportQueue;
        private SaveFileDialog sdExportQueue;
        private OpenFileDialog odExportQueue;
        private Button btAppendQueue;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton tsbCopyAllFlat;
        private Ookii.Dialogs.WinForms.VistaFolderBrowserDialog fbdDirectory;
        private SplitContainer scQueue;
        private ListBox lbSongs;
    }
}