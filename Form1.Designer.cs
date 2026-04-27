namespace io_lockdown
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.RichTextBox rtbLogs;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblVersion;
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

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.rtbLogs = new System.Windows.Forms.RichTextBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            this.grpBluetooth = new System.Windows.Forms.GroupBox();
            this.btnRefreshBluetooth = new System.Windows.Forms.Button();
            this.lblBtInfo = new System.Windows.Forms.Label();
            this.btnSaveBluetooth = new System.Windows.Forms.Button();
            this.cmbBluetoothDevices = new System.Windows.Forms.ComboBox();
            this.grpWhitelist = new System.Windows.Forms.GroupBox();
            this.lstWhitelist = new System.Windows.Forms.ListBox();
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
            this.lblStatus.Location = new System.Drawing.Point(12, 9);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(210, 21);
            this.lblStatus.TabIndex = 1;
            this.lblStatus.Text = "Status: Proteção Ativada";
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
            this.lblVersion.Text = "v1.1.7";
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
            this.lblBtInfo.Text = "Selecione o dispositivo Bluetooth pareado. O sistema bloqueará o PC caso ele se a" +
    "faste.";
            // 
            // btnSaveBluetooth
            // 
            this.btnSaveBluetooth.Location = new System.Drawing.Point(265, 22);
            this.btnSaveBluetooth.Name = "btnSaveBluetooth";
            this.btnSaveBluetooth.Size = new System.Drawing.Size(99, 25);
            this.btnSaveBluetooth.TabIndex = 1;
            this.btnSaveBluetooth.Text = "Ativar Monitor";
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
            this.grpWhitelist.Text = "Whitelist Global de Hardware (PnP)";
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
            this.Controls.Add(this.lblVersion);
            this.Controls.Add(this.grpWhitelist);
            this.Controls.Add(this.grpBluetooth);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.rtbLogs);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("Icon1")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "I/O Lockdown - Console de Auditoria";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.grpBluetooth.ResumeLayout(false);
            this.grpWhitelist.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
