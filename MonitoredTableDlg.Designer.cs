namespace Quickstarts.DataAccessClient
{
    partial class MonitoredTableDlg
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            this.TopPanel = new System.Windows.Forms.Panel();
            this.CloseBTN = new System.Windows.Forms.Button();
            this.ApplyScanBTN = new System.Windows.Forms.Button();
            this.ScanMsNUD = new System.Windows.Forms.NumericUpDown();
            this.ScanLB = new System.Windows.Forms.Label();
            this.InfoLB = new System.Windows.Forms.Label();
            this.ItemsGV = new System.Windows.Forms.DataGridView();
            this.NameCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ValueCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.QualityCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.UpdatedCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TopPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ScanMsNUD)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ItemsGV)).BeginInit();
            this.SuspendLayout();
            //
            // TopPanel
            //
            this.TopPanel.Controls.Add(this.InfoLB);
            this.TopPanel.Controls.Add(this.ScanLB);
            this.TopPanel.Controls.Add(this.ScanMsNUD);
            this.TopPanel.Controls.Add(this.ApplyScanBTN);
            this.TopPanel.Controls.Add(this.CloseBTN);
            this.TopPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.TopPanel.Location = new System.Drawing.Point(0, 0);
            this.TopPanel.Name = "TopPanel";
            this.TopPanel.Size = new System.Drawing.Size(900, 46);
            this.TopPanel.TabIndex = 0;
            //
            // CloseBTN
            //
            this.CloseBTN.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.CloseBTN.Location = new System.Drawing.Point(806, 10);
            this.CloseBTN.Name = "CloseBTN";
            this.CloseBTN.Size = new System.Drawing.Size(82, 25);
            this.CloseBTN.TabIndex = 4;
            this.CloseBTN.Text = "Close";
            this.CloseBTN.UseVisualStyleBackColor = true;
            this.CloseBTN.Click += new System.EventHandler(this.CloseBTN_Click);
            //
            // ApplyScanBTN
            //
            this.ApplyScanBTN.Location = new System.Drawing.Point(210, 10);
            this.ApplyScanBTN.Name = "ApplyScanBTN";
            this.ApplyScanBTN.Size = new System.Drawing.Size(110, 25);
            this.ApplyScanBTN.TabIndex = 3;
            this.ApplyScanBTN.Text = "Apply scan time";
            this.ApplyScanBTN.UseVisualStyleBackColor = true;
            this.ApplyScanBTN.Click += new System.EventHandler(this.ApplyScanBTN_Click);
            //
            // ScanMsNUD
            //
            this.ScanMsNUD.Increment = new decimal(new int[] { 100, 0, 0, 0 });
            this.ScanMsNUD.Location = new System.Drawing.Point(105, 13);
            this.ScanMsNUD.Maximum = new decimal(new int[] { 600000, 0, 0, 0 });
            this.ScanMsNUD.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            this.ScanMsNUD.Name = "ScanMsNUD";
            this.ScanMsNUD.Size = new System.Drawing.Size(90, 20);
            this.ScanMsNUD.TabIndex = 2;
            this.ScanMsNUD.Value = new decimal(new int[] { 1000, 0, 0, 0 });
            //
            // ScanLB
            //
            this.ScanLB.AutoSize = true;
            this.ScanLB.Location = new System.Drawing.Point(12, 15);
            this.ScanLB.Name = "ScanLB";
            this.ScanLB.Size = new System.Drawing.Size(87, 13);
            this.ScanLB.TabIndex = 1;
            this.ScanLB.Text = "Scan time (ms):";
            //
            // InfoLB
            //
            this.InfoLB.AutoEllipsis = true;
            this.InfoLB.Location = new System.Drawing.Point(12, 30);
            this.InfoLB.Name = "InfoLB";
            this.InfoLB.Size = new System.Drawing.Size(780, 15);
            this.InfoLB.TabIndex = 0;
            this.InfoLB.Text = "Connecting...";
            //
            // ItemsGV
            //
            this.ItemsGV.AllowUserToAddRows = false;
            this.ItemsGV.AllowUserToDeleteRows = false;
            this.ItemsGV.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.ItemsGV.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.NameCol,
            this.ValueCol,
            this.QualityCol,
            this.UpdatedCol});
            this.ItemsGV.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ItemsGV.Location = new System.Drawing.Point(0, 46);
            this.ItemsGV.Name = "ItemsGV";
            this.ItemsGV.RowHeadersVisible = false;
            this.ItemsGV.Size = new System.Drawing.Size(900, 554);
            this.ItemsGV.TabIndex = 1;
            this.ItemsGV.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.ItemsGV_CellEndEdit);
            this.ItemsGV.CellBeginEdit += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.ItemsGV_CellBeginEdit);
            //
            // NameCol
            //
            this.NameCol.HeaderText = "Tag";
            this.NameCol.Name = "NameCol";
            this.NameCol.ReadOnly = true;
            this.NameCol.Width = 320;
            //
            // ValueCol
            //
            this.ValueCol.HeaderText = "Value";
            this.ValueCol.Name = "ValueCol";
            this.ValueCol.Width = 150;
            //
            // QualityCol
            //
            this.QualityCol.HeaderText = "Quality";
            this.QualityCol.Name = "QualityCol";
            this.QualityCol.ReadOnly = true;
            this.QualityCol.Width = 120;
            //
            // UpdatedCol
            //
            this.UpdatedCol.HeaderText = "Updated";
            this.UpdatedCol.Name = "UpdatedCol";
            this.UpdatedCol.ReadOnly = true;
            this.UpdatedCol.Width = 100;
            //
            // MonitoredTableDlg
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 600);
            this.Controls.Add(this.ItemsGV);
            this.Controls.Add(this.TopPanel);
            this.MinimumSize = new System.Drawing.Size(700, 400);
            this.Name = "MonitoredTableDlg";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Monitored Table";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MonitoredTableDlg_FormClosing);
            this.TopPanel.ResumeLayout(false);
            this.TopPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ScanMsNUD)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ItemsGV)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel TopPanel;
        private System.Windows.Forms.Label InfoLB;
        private System.Windows.Forms.Label ScanLB;
        private System.Windows.Forms.NumericUpDown ScanMsNUD;
        private System.Windows.Forms.Button ApplyScanBTN;
        private System.Windows.Forms.Button CloseBTN;
        private System.Windows.Forms.DataGridView ItemsGV;
        private System.Windows.Forms.DataGridViewTextBoxColumn NameCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn ValueCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn QualityCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn UpdatedCol;
    }
}
