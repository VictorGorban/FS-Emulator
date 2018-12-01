namespace FS_Emulator
{
	partial class FormModifyFile
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
			this.btSave = new System.Windows.Forms.Button();
			this.tbFileData = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// btSave
			// 
			this.btSave.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.btSave.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btSave.Location = new System.Drawing.Point(314, 261);
			this.btSave.Name = "btSave";
			this.btSave.Size = new System.Drawing.Size(188, 23);
			this.btSave.TabIndex = 3;
			this.btSave.Text = "Сохранить";
			this.btSave.UseVisualStyleBackColor = true;
			this.btSave.Click += new System.EventHandler(this.btSave_Click);
			// 
			// tbFileData
			// 
			this.tbFileData.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tbFileData.Location = new System.Drawing.Point(1, 3);
			this.tbFileData.Multiline = true;
			this.tbFileData.Name = "tbFileData";
			this.tbFileData.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.tbFileData.Size = new System.Drawing.Size(853, 254);
			this.tbFileData.TabIndex = 2;
			// 
			// FormModifyFile
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(854, 288);
			this.Controls.Add(this.btSave);
			this.Controls.Add(this.tbFileData);
			this.Name = "FormModifyFile";
			this.Text = "Данные файла";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btSave;
		private System.Windows.Forms.TextBox tbFileData;
	}
}