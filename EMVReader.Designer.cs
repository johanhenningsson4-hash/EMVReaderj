namespace EMVCard
{
    partial class MainEMVReaderBin
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
            this.richTextBoxLogs = new System.Windows.Forms.RichTextBox();
            this.textCardNum = new System.Windows.Forms.TextBox();
            this.textEXP = new System.Windows.Forms.TextBox();
            this.textHolder = new System.Windows.Forms.TextBox();
            this.textTrack = new System.Windows.Forms.TextBox();
            this.cbReader = new System.Windows.Forms.ComboBox();
            this.cbPSE = new System.Windows.Forms.ComboBox();
            this.bInit = new System.Windows.Forms.Button();
            this.bConnect = new System.Windows.Forms.Button();
            this.bReset = new System.Windows.Forms.Button();
            this.bClear = new System.Windows.Forms.Button();
            this.bLoadPSE = new System.Windows.Forms.Button();
            this.bLoadPPSE = new System.Windows.Forms.Button();
            this.bReadApp = new System.Windows.Forms.Button();
            this.bQuit = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // richTextBoxLogs
            // 
            this.richTextBoxLogs.Location = new System.Drawing.Point(12, 12);
            this.richTextBoxLogs.Name = "richTextBoxLogs";
            this.richTextBoxLogs.Size = new System.Drawing.Size(600, 300);
            this.richTextBoxLogs.TabIndex = 0;
            this.richTextBoxLogs.Text = "";
            // 
            // textCardNum
            // 
            this.textCardNum.Location = new System.Drawing.Point(12, 330);
            this.textCardNum.Name = "textCardNum";
            this.textCardNum.Size = new System.Drawing.Size(200, 20);
            this.textCardNum.TabIndex = 1;
            // 
            // textEXP
            // 
            this.textEXP.Location = new System.Drawing.Point(230, 330);
            this.textEXP.Name = "textEXP";
            this.textEXP.Size = new System.Drawing.Size(100, 20);
            this.textEXP.TabIndex = 2;
            // 
            // textHolder
            // 
            this.textHolder.Location = new System.Drawing.Point(350, 330);
            this.textHolder.Name = "textHolder";
            this.textHolder.Size = new System.Drawing.Size(150, 20);
            this.textHolder.TabIndex = 3;
            // 
            // textTrack
            // 
            this.textTrack.Location = new System.Drawing.Point(12, 370);
            this.textTrack.Name = "textTrack";
            this.textTrack.Size = new System.Drawing.Size(600, 20);
            this.textTrack.TabIndex = 4;
            // 
            // cbReader
            // 
            this.cbReader.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbReader.Location = new System.Drawing.Point(12, 410);
            this.cbReader.Name = "cbReader";
            this.cbReader.Size = new System.Drawing.Size(200, 21);
            this.cbReader.TabIndex = 5;
            // 
            // cbPSE
            // 
            this.cbPSE.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbPSE.Location = new System.Drawing.Point(230, 410);
            this.cbPSE.Name = "cbPSE";
            this.cbPSE.Size = new System.Drawing.Size(200, 21);
            this.cbPSE.TabIndex = 6;
            // 
            // bInit
            // 
            this.bInit.Location = new System.Drawing.Point(12, 450);
            this.bInit.Name = "bInit";
            this.bInit.Size = new System.Drawing.Size(75, 23);
            this.bInit.TabIndex = 7;
            this.bInit.Text = "Init";
            this.bInit.UseVisualStyleBackColor = true;
            this.bInit.Click += new System.EventHandler(this.bInit_Click);
            // 
            // bConnect
            // 
            this.bConnect.Enabled = false;
            this.bConnect.Location = new System.Drawing.Point(100, 450);
            this.bConnect.Name = "bConnect";
            this.bConnect.Size = new System.Drawing.Size(75, 23);
            this.bConnect.TabIndex = 8;
            this.bConnect.Text = "Connect";
            this.bConnect.UseVisualStyleBackColor = true;
            this.bConnect.Click += new System.EventHandler(this.bConnect_Click);
            // 
            // bLoadPSE
            // 
            this.bLoadPSE.Enabled = false;
            this.bLoadPSE.Location = new System.Drawing.Point(190, 450);
            this.bLoadPSE.Name = "bLoadPSE";
            this.bLoadPSE.Size = new System.Drawing.Size(75, 23);
            this.bLoadPSE.TabIndex = 9;
            this.bLoadPSE.Text = "Load PSE";
            this.bLoadPSE.UseVisualStyleBackColor = true;
            this.bLoadPSE.Click += new System.EventHandler(this.bLoadPSE_Click);
            // 
            // bLoadPPSE
            // 
            this.bLoadPPSE.Enabled = false;
            this.bLoadPPSE.Location = new System.Drawing.Point(280, 450);
            this.bLoadPPSE.Name = "bLoadPPSE";
            this.bLoadPPSE.Size = new System.Drawing.Size(75, 23);
            this.bLoadPPSE.TabIndex = 10;
            this.bLoadPPSE.Text = "Load PPSE";
            this.bLoadPPSE.UseVisualStyleBackColor = true;
            this.bLoadPPSE.Click += new System.EventHandler(this.bLoadPPSE_Click);
            // 
            // bReadApp
            // 
            this.bReadApp.Enabled = false;
            this.bReadApp.Location = new System.Drawing.Point(370, 450);
            this.bReadApp.Name = "bReadApp";
            this.bReadApp.Size = new System.Drawing.Size(75, 23);
            this.bReadApp.TabIndex = 11;
            this.bReadApp.Text = "Read App";
            this.bReadApp.UseVisualStyleBackColor = true;
            this.bReadApp.Click += new System.EventHandler(this.bReadApp_Click);
            // 
            // bReset
            // 
            this.bReset.Enabled = false;
            this.bReset.Location = new System.Drawing.Point(460, 450);
            this.bReset.Name = "bReset";
            this.bReset.Size = new System.Drawing.Size(75, 23);
            this.bReset.TabIndex = 12;
            this.bReset.Text = "Reset";
            this.bReset.UseVisualStyleBackColor = true;
            this.bReset.Click += new System.EventHandler(this.bReset_Click);
            // 
            // bClear
            // 
            this.bClear.Enabled = false;
            this.bClear.Location = new System.Drawing.Point(12, 490);
            this.bClear.Name = "bClear";
            this.bClear.Size = new System.Drawing.Size(75, 23);
            this.bClear.TabIndex = 13;
            this.bClear.Text = "Clear";
            this.bClear.UseVisualStyleBackColor = true;
            this.bClear.Click += new System.EventHandler(this.bClear_Click);
            // 
            // bQuit
            // 
            this.bQuit.Location = new System.Drawing.Point(537, 490);
            this.bQuit.Name = "bQuit";
            this.bQuit.Size = new System.Drawing.Size(75, 23);
            this.bQuit.TabIndex = 14;
            this.bQuit.Text = "Quit";
            this.bQuit.UseVisualStyleBackColor = true;
            this.bQuit.Click += new System.EventHandler(this.bQuit_Click);
            // 
            // MainEMVReaderBin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(624, 525);
            this.Controls.Add(this.bQuit);
            this.Controls.Add(this.bClear);
            this.Controls.Add(this.bReset);
            this.Controls.Add(this.bReadApp);
            this.Controls.Add(this.bLoadPPSE);
            this.Controls.Add(this.bLoadPSE);
            this.Controls.Add(this.bConnect);
            this.Controls.Add(this.bInit);
            this.Controls.Add(this.cbPSE);
            this.Controls.Add(this.cbReader);
            this.Controls.Add(this.textTrack);
            this.Controls.Add(this.textHolder);
            this.Controls.Add(this.textEXP);
            this.Controls.Add(this.textCardNum);
            this.Controls.Add(this.richTextBoxLogs);
            this.Name = "MainEMVReaderBin";
            this.Text = "EMV Card Reader v2.1.0";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.RichTextBox richTextBoxLogs;
        private System.Windows.Forms.TextBox textCardNum;
        private System.Windows.Forms.TextBox textEXP;
        private System.Windows.Forms.TextBox textHolder;
        private System.Windows.Forms.TextBox textTrack;
        private System.Windows.Forms.ComboBox cbReader;
        private System.Windows.Forms.ComboBox cbPSE;
        private System.Windows.Forms.Button bInit;
        private System.Windows.Forms.Button bConnect;
        private System.Windows.Forms.Button bLoadPSE;
        private System.Windows.Forms.Button bLoadPPSE;
        private System.Windows.Forms.Button bReadApp;
        private System.Windows.Forms.Button bReset;
        private System.Windows.Forms.Button bClear;
        private System.Windows.Forms.Button bQuit;
    }
}