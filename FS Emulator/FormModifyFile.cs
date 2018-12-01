using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FS_Emulator
{
	public partial class FormModifyFile : Form
	{
		string PathToSave;

		public FormModifyFile()
		{
			InitializeComponent();
			//throw new Exception("Надо было вызвать другой конструктор");
		}

		public FormModifyFile(string oldText, bool CanModify = true, string pathToSave = "newFileData.txt"):this()
		{
			tbFileData.Text = oldText;
			btSave.Enabled = CanModify;
			btSave.Text = CanModify ? "Сохранить" : "Вы не можете изменять этот файл";

			PathToSave = pathToSave;
		}

		private void btSave_Click(object sender, EventArgs e)
		{
			string newText = tbFileData.Text;
			using (TextWriter writer = new StreamWriter(PathToSave))
			{
				writer.Write(newText);
			}

			this.Close();
		}
	}
}
