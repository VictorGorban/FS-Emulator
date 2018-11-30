namespace FS_Emulator
{
    partial class MainForm
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
			this.menuMain = new System.Windows.Forms.MenuStrip();
			this.фСToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.fsNewMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.fsOpenMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.форматироватьToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.outputTB = new System.Windows.Forms.TextBox();
			this.inputTB = new System.Windows.Forms.TextBox();
			this.menuMain.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuMain
			// 
			this.menuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.фСToolStripMenuItem});
			this.menuMain.Location = new System.Drawing.Point(0, 0);
			this.menuMain.Name = "menuMain";
			this.menuMain.Size = new System.Drawing.Size(800, 24);
			this.menuMain.TabIndex = 0;
			this.menuMain.Text = "menuStrip1";
			// 
			// фСToolStripMenuItem
			// 
			this.фСToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fsNewMenuItem,
            this.fsOpenMenuItem,
            this.форматироватьToolStripMenuItem});
			this.фСToolStripMenuItem.Name = "фСToolStripMenuItem";
			this.фСToolStripMenuItem.Size = new System.Drawing.Size(36, 20);
			this.фСToolStripMenuItem.Text = "ФС";
			this.фСToolStripMenuItem.Click += new System.EventHandler(this.фСToolStripMenuItem_Click);
			// 
			// fsNewMenuItem
			// 
			this.fsNewMenuItem.Name = "fsNewMenuItem";
			this.fsNewMenuItem.Size = new System.Drawing.Size(161, 22);
			this.fsNewMenuItem.Text = "Новая";
			this.fsNewMenuItem.Click += new System.EventHandler(this.fsNewMenuItem_Click);
			// 
			// fsOpenMenuItem
			// 
			this.fsOpenMenuItem.Name = "fsOpenMenuItem";
			this.fsOpenMenuItem.Size = new System.Drawing.Size(161, 22);
			this.fsOpenMenuItem.Text = "Открыть";
			this.fsOpenMenuItem.Click += new System.EventHandler(this.fsOpenMenuItem_Click);
			// 
			// форматироватьToolStripMenuItem
			// 
			this.форматироватьToolStripMenuItem.Name = "форматироватьToolStripMenuItem";
			this.форматироватьToolStripMenuItem.Size = new System.Drawing.Size(161, 22);
			this.форматироватьToolStripMenuItem.Text = "Форматировать";
			this.форматироватьToolStripMenuItem.Click += new System.EventHandler(this.форматироватьToolStripMenuItem_Click);
			// 
			// outputTB
			// 
			this.outputTB.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.outputTB.BackColor = System.Drawing.Color.Black;
			this.outputTB.Font = new System.Drawing.Font("Lucida Console", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.outputTB.ForeColor = System.Drawing.Color.Green;
			this.outputTB.Location = new System.Drawing.Point(2, 26);
			this.outputTB.Multiline = true;
			this.outputTB.Name = "outputTB";
			this.outputTB.ReadOnly = true;
			this.outputTB.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
			this.outputTB.Size = new System.Drawing.Size(796, 411);
			this.outputTB.TabIndex = 1;
			this.outputTB.TabStop = false;
			this.outputTB.Text = "Откройте файл ФС, чтобы начать";
			this.outputTB.WordWrap = false;
			this.outputTB.TextChanged += new System.EventHandler(this.outputTB_TextChanged);
			// 
			// inputTB
			// 
			this.inputTB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.inputTB.BackColor = System.Drawing.Color.Black;
			this.inputTB.Font = new System.Drawing.Font("Lucida Console", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.inputTB.ForeColor = System.Drawing.Color.Green;
			this.inputTB.Location = new System.Drawing.Point(2, 434);
			this.inputTB.Name = "inputTB";
			this.inputTB.Size = new System.Drawing.Size(796, 21);
			this.inputTB.TabIndex = 2;
			this.inputTB.TabStop = false;
			this.inputTB.KeyDown += new System.Windows.Forms.KeyEventHandler(this.inputTB_KeyDown);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.Tan;
			this.ClientSize = new System.Drawing.Size(800, 460);
			this.Controls.Add(this.inputTB);
			this.Controls.Add(this.outputTB);
			this.Controls.Add(this.menuMain);
			this.MainMenuStrip = this.menuMain;
			this.Name = "MainForm";
			this.Text = "Form1";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.menuMain.ResumeLayout(false);
			this.menuMain.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuMain;
        private System.Windows.Forms.ToolStripMenuItem фСToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fsNewMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fsOpenMenuItem;
        private System.Windows.Forms.TextBox outputTB;
        private System.Windows.Forms.TextBox inputTB;
		private System.Windows.Forms.ToolStripMenuItem форматироватьToolStripMenuItem;
	}
}

