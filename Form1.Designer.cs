namespace io_lockdown
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.RichTextBox rtbLogs;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.PictureBox picLogo;
        private System.Windows.Forms.GroupBox grpBluetooth;
        private System.Windows.Forms.ComboBox cmbBluetoothDevices;
        private System.Windows.Forms.Button btnSaveBluetooth;
        private System.Windows.Forms.Button btnRefreshBluetooth;
        private System.Windows.Forms.Label lblBtInfo;
        private System.Windows.Forms.GroupBox grpWhitelist;
        private System.Windows.Forms.ListBox lstWhitelist;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private System.Windows.Forms.Button btnReset;

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.rtbLogs = new System.Windows.Forms.RichTextBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnReset = new System.Windows.Forms.Button();
            this.lblVersion = new System.Windows.Forms.Label();
            this.picLogo = new System.Windows.Forms.PictureBox();
            this.grpBluetooth = new System.Windows.Forms.GroupBox();
            this.btnRefreshBluetooth = new System.Windows.Forms.Button();
            this.lblBtInfo = new System.Windows.Forms.Label();
            this.btnSaveBluetooth = new System.Windows.Forms.Button();
            this.cmbBluetoothDevices = new System.Windows.Forms.ComboBox();
            this.grpWhitelist = new System.Windows.Forms.GroupBox();
            this.lstWhitelist = new System.Windows.Forms.ListBox();
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).BeginInit();
            this.grpBluetooth.SuspendLayout();
            this.grpWhitelist.SuspendLayout();
            this.SuspendLayout();
            // 
            // rtbLogs
            // 
            this.rtbLogs.BackColor = System.Drawing.Color.Black;
            this.rtbLogs.ForeColor = System.Drawing.Color.Lime;
            this.rtbLogs.Location = new System.Drawing.Point(12, 175);
            this.rtbLogs.Name = "rtbLogs";
            this.rtbLogs.ReadOnly = true;
            this.rtbLogs.Size = new System.Drawing.Size(760, 274);
            this.rtbLogs.TabIndex = 0;
            this.rtbLogs.Text = "";
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblStatus.Location = new System.Drawing.Point(50, 9);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(210, 21);
            this.lblStatus.TabIndex = 1;
            this.lblStatus.Text = "Status: Protection Active";
            // 
            // btnReset
            // 
            this.btnReset.BackColor = System.Drawing.Color.DarkRed;
            this.btnReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReset.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnReset.ForeColor = System.Drawing.Color.White;
            this.btnReset.Location = new System.Drawing.Point(450, 7);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(120, 27);
            this.btnReset.TabIndex = 6;
            this.btnReset.Text = "RESET LOCKDOWN";
            this.btnReset.UseVisualStyleBackColor = false;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // picLogo
            // 
            this.picLogo.Location = new System.Drawing.Point(12, 5);
            this.picLogo.Name = "picLogo";
            this.picLogo.Size = new System.Drawing.Size(32, 32);
            this.picLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picLogo.TabIndex = 5;
            this.picLogo.TabStop = false;
            if (System.IO.File.Exists("logo.png")) this.picLogo.Image = System.Drawing.Image.FromFile("logo.png");
            // 
            // lblVersion
            // 
            this.lblVersion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblVersion.AutoSize = true;
            this.lblVersion.ForeColor = System.Drawing.Color.Gray;
            this.lblVersion.Location = new System.Drawing.Point(720, 14);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(43, 15);
            this.lblVersion.TabIndex = 4;
            this.lblVersion.Text = "v1.1.9";
            // 
            // grpBluetooth
            // 
            this.grpBluetooth.Controls.Add(this.btnRefreshBluetooth);
            this.grpBluetooth.Controls.Add(this.lblBtInfo);
            this.grpBluetooth.Controls.Add(this.btnSaveBluetooth);
            this.grpBluetooth.Controls.Add(this.cmbBluetoothDevices);
            this.grpBluetooth.Location = new System.Drawing.Point(12, 40);
            this.grpBluetooth.Name = "grpBluetooth";
            this.grpBluetooth.Size = new System.Drawing.Size(370, 120);
            this.grpBluetooth.TabIndex = 2;
            this.grpBluetooth.TabStop = false;
            this.grpBluetooth.Text = "Smart Lock (Bluetooth)";
            // 
            // btnRefreshBluetooth
            // 
            this.btnRefreshBluetooth.Location = new System.Drawing.Point(230, 22);
            this.btnRefreshBluetooth.Name = "btnRefreshBluetooth";
            this.btnRefreshBluetooth.Size = new System.Drawing.Size(30, 25);
            this.btnRefreshBluetooth.TabIndex = 3;
            this.btnRefreshBluetooth.Text = "🔄";
            this.btnRefreshBluetooth.UseVisualStyleBackColor = true;
            this.btnRefreshBluetooth.Click += new System.EventHandler(this.btnRefreshBluetooth_Click);
            // 
            // lblBtInfo
            // 
            this.lblBtInfo.Location = new System.Drawing.Point(6, 60);
            this.lblBtInfo.Name = "lblBtInfo";
            this.lblBtInfo.Size = new System.Drawing.Size(358, 50);
            this.lblBtInfo.TabIndex = 2;
            this.lblBtInfo.Text = "Select the paired Bluetooth device. The system will lock the PC if it moves away.";
            // 
            // btnSaveBluetooth
            // 
            this.btnSaveBluetooth.Location = new System.Drawing.Point(265, 22);
            this.btnSaveBluetooth.Name = "btnSaveBluetooth";
            this.btnSaveBluetooth.Size = new System.Drawing.Size(99, 25);
            this.btnSaveBluetooth.TabIndex = 1;
            this.btnSaveBluetooth.Text = "Enable Monitor";
            this.btnSaveBluetooth.UseVisualStyleBackColor = true;
            this.btnSaveBluetooth.Click += new System.EventHandler(this.btnSaveBluetooth_Click);
            // 
            // cmbBluetoothDevices
            // 
            this.cmbBluetoothDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBluetoothDevices.FormattingEnabled = true;
            this.cmbBluetoothDevices.Location = new System.Drawing.Point(10, 23);
            this.cmbBluetoothDevices.Name = "cmbBluetoothDevices";
            this.cmbBluetoothDevices.Size = new System.Drawing.Size(214, 23);
            this.cmbBluetoothDevices.TabIndex = 0;
            // 
            // grpWhitelist
            // 
            this.grpWhitelist.Controls.Add(this.lstWhitelist);
            this.grpWhitelist.Location = new System.Drawing.Point(395, 40);
            this.grpWhitelist.Name = "grpWhitelist";
            this.grpWhitelist.Size = new System.Drawing.Size(377, 120);
            this.grpWhitelist.TabIndex = 3;
            this.grpWhitelist.TabStop = false;
            this.grpWhitelist.Text = "Global Hardware Whitelist (PnP)";
            // 
            // lstWhitelist
            // 
            this.lstWhitelist.FormattingEnabled = true;
            this.lstWhitelist.ItemHeight = 15;
            this.lstWhitelist.Location = new System.Drawing.Point(10, 22);
            this.lstWhitelist.Name = "lstWhitelist";
            this.lstWhitelist.Size = new System.Drawing.Size(357, 79);
            this.lstWhitelist.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 461);
            this.Controls.Add(this.btnReset);
            this.Controls.Add(this.picLogo);
            this.Controls.Add(this.lblVersion);
            this.Controls.Add(this.grpWhitelist);
            this.Controls.Add(this.grpBluetooth);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.rtbLogs);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("Icon1")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "I/O Lockdown - Audit Console";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.picLogo)).EndInit();
            this.grpBluetooth.ResumeLayout(false);
            this.grpWhitelist.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
