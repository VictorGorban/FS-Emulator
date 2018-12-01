namespace FS_Emulator
{
    partial class FormFormatFS
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
			this.label1 = new System.Windows.Forms.Label();
			this.cbox_ClusterSize = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.PathToSaveTB = new System.Windows.Forms.TextBox();
			this.BtSelectPathToSave = new System.Windows.Forms.Button();
			this.BtOK = new System.Windows.Forms.Button();
			this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
			this.TB_MB = new System.Windows.Forms.TextBox();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(30, 55);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(54, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Емкость:";
			// 
			// cbox_ClusterSize
			// 
			this.cbox_ClusterSize.FormattingEnabled = true;
			this.cbox_ClusterSize.Location = new System.Drawing.Point(165, 87);
			this.cbox_ClusterSize.Name = "cbox_ClusterSize";
			this.cbox_ClusterSize.Size = new System.Drawing.Size(61, 21);
			this.cbox_ClusterSize.TabIndex = 4;
			this.cbox_ClusterSize.SelectedValueChanged += new System.EventHandler(this.cbox_ClusterSize_SelectedValueChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(30, 90);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(99, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "Размер кластера:";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(30, 122);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(87, 13);
			this.label3.TabIndex = 6;
			this.label3.Text = "Как сохранить?";
			// 
			// PathToSaveTB
			// 
			this.PathToSaveTB.BackColor = System.Drawing.Color.WhiteSmoke;
			this.PathToSaveTB.Location = new System.Drawing.Point(33, 144);
			this.PathToSaveTB.Name = "PathToSaveTB";
			this.PathToSaveTB.ReadOnly = true;
			this.PathToSaveTB.Size = new System.Drawing.Size(193, 20);
			this.PathToSaveTB.TabIndex = 5;
			this.PathToSaveTB.Text = "E:\\ForFS\\FS";
			this.PathToSaveTB.TextChanged += new System.EventHandler(this.PathToSaveTB_TextChanged);
			// 
			// BtSelectPathToSave
			// 
			this.BtSelectPathToSave.Location = new System.Drawing.Point(33, 170);
			this.BtSelectPathToSave.Name = "BtSelectPathToSave";
			this.BtSelectPathToSave.Size = new System.Drawing.Size(75, 22);
			this.BtSelectPathToSave.TabIndex = 7;
			this.BtSelectPathToSave.Text = "Выбрать";
			this.BtSelectPathToSave.UseVisualStyleBackColor = true;
			this.BtSelectPathToSave.Click += new System.EventHandler(this.TBSelectPathToSave_Click);
			// 
			// BtOK
			// 
			this.BtOK.Location = new System.Drawing.Point(33, 230);
			this.BtOK.Name = "BtOK";
			this.BtOK.Size = new System.Drawing.Size(193, 22);
			this.BtOK.TabIndex = 8;
			this.BtOK.Text = "Ок";
			this.BtOK.UseVisualStyleBackColor = true;
			this.BtOK.Click += new System.EventHandler(this.BtOK_Click);
			// 
			// numericUpDown1
			// 
			this.numericUpDown1.Location = new System.Drawing.Point(116, 53);
			this.numericUpDown1.Maximum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
			this.numericUpDown1.Minimum = new decimal(new int[] {
            4,
            0,
            0,
            0});
			this.numericUpDown1.Name = "numericUpDown1";
			this.numericUpDown1.Size = new System.Drawing.Size(70, 20);
			this.numericUpDown1.TabIndex = 9;
			this.numericUpDown1.Value = new decimal(new int[] {
            4,
            0,
            0,
            0});
			this.numericUpDown1.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
			// 
			// TB_MB
			// 
			this.TB_MB.BackColor = System.Drawing.Color.WhiteSmoke;
			this.TB_MB.Location = new System.Drawing.Point(190, 53);
			this.TB_MB.Name = "TB_MB";
			this.TB_MB.ReadOnly = true;
			this.TB_MB.Size = new System.Drawing.Size(36, 20);
			this.TB_MB.TabIndex = 10;
			this.TB_MB.Text = "MB";
			// 
			// FormFormatFS
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(261, 294);
			this.Controls.Add(this.TB_MB);
			this.Controls.Add(this.numericUpDown1);
			this.Controls.Add(this.BtOK);
			this.Controls.Add(this.BtSelectPathToSave);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.PathToSaveTB);
			this.Controls.Add(this.cbox_ClusterSize);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Name = "FormFormatFS";
			this.Text = "Новая ФС";
			this.Load += new System.EventHandler(this.FormCreateFS_Load);
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbox_ClusterSize;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox PathToSaveTB;
        private System.Windows.Forms.Button BtSelectPathToSave;
        private System.Windows.Forms.Button BtOK;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.TextBox TB_MB;
	}
}