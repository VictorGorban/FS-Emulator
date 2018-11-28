﻿using FS_Emulator.FSTools;
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
using static System.Environment;

namespace FS_Emulator
{
    public partial class MainForm : Form
    {
		string thisFSPath = "";
		string helloText = @"Привет! Чтобы начать работу, войдите в систему";

		public int CurrentDirId = 2; // Да, в идеале каждого отдельного юзера посылать в его директорию... Но это уже пространство для улучшения. Потом.


		Dictionary<string, KeyValuePair<int, Func<string[], string[]> >> Commands = new Dictionary<string, KeyValuePair<int, Func<string[], string[]>>>();
		// [Dir] => if key == args.Length then [key]


        public MainForm()
        {
            InitializeComponent();
			this.WindowState = FormWindowState.Maximized;

			Commands = new Dictionary<string, KeyValuePair<int, Func<string[], string[]>>>
			{
				["dir"] = new KeyValuePair<int, Func<string[], string[]>>(0, WorkWithFS.GetFilesInCurrentDir),
				["users"] = new KeyValuePair<int, Func<string[], string[]>>(0, WorkWithFS.GetAllUsers),
				["createdir"] = new KeyValuePair<int, Func<string[], string[]>>(1, WorkWithFS.CreateDir),
				["createfile"] = new KeyValuePair<int, Func<string[], string[]>>(1, WorkWithFS.CreateFile),
				["createuser"] = new KeyValuePair<int, Func<string[], string[]>>(3, WorkWithFS.CreateUser),
				["rename"] = new KeyValuePair<int, Func<string[], string[]>>(2, WorkWithFS.Rename),
				["goto"] = new KeyValuePair<int, Func<string[], string[]>>(1, WorkWithFS.GoTo),
				["open"] = new KeyValuePair<int, Func<string[], string[]>>(1, WorkWithFS.OpenFile)

			};

        }

        private void Form1_Load(object sender, EventArgs e)
        {
			// открываю ФС
			thisFSPath = @"E:\ForFS\FS";
			/*WorkWithFS.fsToWork = new FS(thisFSPath);

			outputTB.Clear();
			outputTB.AppendText(helloText + NewLine);*/

		}

        private void fsNewMenuItem_Click(object sender, EventArgs e)
        {
            var form = new FormFormatFS();
            form.ShowDialog(); // там по кнопке OK создается FS
        }

		private void outputTB_TextChanged(object sender, EventArgs e)
		{
			
		}

		private void fsOpenMenuItem_Click(object sender, EventArgs e)
		{
			var dialog = new OpenFileDialog()
			{
				CheckPathExists = true,
				InitialDirectory = @"E:\ForFS",
				Multiselect = false
			};
			var dialogResult = dialog.ShowDialog();

			if (dialogResult == DialogResult.OK)
			{
				//WorkWithFS.fsToWork?.stream?.Dispose();
				thisFSPath = dialog.FileName;
				WorkWithFS.fsToWork = new FS(dialog.FileName);

				outputTB.Clear();
				outputTB.AppendText(helloText+NewLine);
			}

		}

		private void форматироватьToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(thisFSPath))
			{
				MessageBox.Show("Сначала откройте файл с ФС");
				return;
			}

			WorkWithFS.fsToWork.Close();

			var form = new FormFormatFS(thisFSPath);
			form.ShowDialog();

			WorkWithFS.fsToWork.Open(thisFSPath);
		}

		private void фСToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}

		private void inputTB_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == 13)
			{
				var tbox = sender as TextBox;
				var command = tbox.Text;
				PerformCommand(command);
				tbox.Clear();
			}
		}

		private void PerformCommand(string strCommand)
		{
			Log(strCommand);

			var args = strCommand.Split(new char[]{ ' '}, StringSplitOptions.RemoveEmptyEntries);
			var parameters = args.Skip(1).ToArray();

			if (args.Length != 0)
			{
				var firstArg = args[0].ToLower();

				if (!Commands.Keys.Contains(firstArg))
				{
					Log("Неверная команда!");
					return;
				}

				var paramsAndCommand = Commands[firstArg];

				int requiredParamsCount = paramsAndCommand.Key;
				if (requiredParamsCount != parameters.Length)
				{
					Log("Неверное число параметров!");
					return;
				}

				var command = paramsAndCommand.Value;
				string[] results = command.Invoke(parameters);
				Log(results);


			}
			
		}

		private void Log(string message)
		{
			outputTB.AppendText(message + NewLine);
		}

		private void Log(string[] messages)
		{
			foreach(var msg in messages)
			{
				Log(msg);
			}
		}

		public static class WorkWithFS
		{
			public static int currentDirIndex = 2;
			public static FS fsToWork = null;

			/// <param name="_">Пустой массив параметров.</param>
			/// <returns>Краткая информация о каждом файле в текущей директории</returns>
			public static string[] GetFilesInCurrentDir(string[] _)
			{
				return fsToWork.GetExistingFilesInDirInShortForm(currentDirIndex);
			}

			/// <param name="_">Пустой массив параметров.</param>
			/// <returns>Краткая информация о каждом пользователе</returns>
			public static string[] GetAllUsers(string[] _)
			{
				return fsToWork.GetAllExistingUsersStrings();
			}

			internal static string[] CreateDir(string[] arg)
			{
				// не забываем о возвращении ошибок...
				// Например, нельзя включать пробелы и /. Почему пробелы - не помню.
				throw new NotImplementedException();
			}

			internal static string[] CreateFile(string[] arg)
			{
				throw new NotImplementedException();
			}

			internal static string[] CreateUser(string[] arg)
			{
				throw new NotImplementedException();
			}

			internal static string[] Rename(string[] arg)
			{
				throw new NotImplementedException();
			}

			internal static string[] GoTo(string[] arg)
			{
				throw new NotImplementedException();
			}

			internal static string[] OpenFile(string[] arg)
			{
				throw new NotImplementedException();
			}
		}

		/*private void LogResult(string result)
		{
			outputTB.AppendText(result);
		}

		private void LogResults(string[] results)
		{
			foreach (var res in results)
			{
				LogResult(res);
			}
		}*/
	}
}
