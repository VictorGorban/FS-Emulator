using FS_Emulator.FSTools;
using FS_Emulator.FSTools.Structs;
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
		private string thisFSPath = "";
		private string helloText = @"Привет! Чтобы начать работу, введите любую команду";
		private Dictionary<string, KeyValuePair<int, Func<string[], string[]>>> Commands = new Dictionary<string, KeyValuePair<int, Func<string[], string[]>>>();
		// [Dir] => if key == args.Length then [key]

		private List<string> buffer = new List<string>();
		private int bufIndex = 0;

		public MainForm()
		{
			InitializeComponent();
			this.WindowState = FormWindowState.Maximized;

			Commands = new Dictionary<string, KeyValuePair<int, Func<string[], string[]>>>
			{
				/**/
				["dir"] = new KeyValuePair<int, Func<string[], string[]>>(0, WorkWithFS.GetFilesInCurrentDir),
				/**/
				["users"] = new KeyValuePair<int, Func<string[], string[]>>(0, WorkWithFS.GetAllUsers),
				/**/
				["createdir"] = new KeyValuePair<int, Func<string[], string[]>>(1, WorkWithFS.CreateDir),
				/**/
				["createfile"] = new KeyValuePair<int, Func<string[], string[]>>(1, WorkWithFS.CreateFile),
				/**/
				["createuser"] = new KeyValuePair<int, Func<string[], string[]>>(3, WorkWithFS.CreateUser),
				/**/
				["rename"] = new KeyValuePair<int, Func<string[], string[]>>(2, WorkWithFS.Rename),
				/**/
				["goto"] = new KeyValuePair<int, Func<string[], string[]>>(1, WorkWithFS.GoTo),
				["open"] = new KeyValuePair<int, Func<string[], string[]>>(1, WorkWithFS.OpenFile),
				/**/
				["login"] = new KeyValuePair<int, Func<string[], string[]>>(2, WorkWithFS.Login),
				/**/
				["logout"] = new KeyValuePair<int, Func<string[], string[]>>(0, WorkWithFS.Logout),
				/**/
				["whoami"] = new KeyValuePair<int, Func<string[], string[]>>(0, WorkWithFS.WhoAmI),
			};

		}

		private void Form1_Load(object sender, EventArgs e)
		{
			// открываю ФС
			thisFSPath = @"E:\ForFS\FS";
			WorkWithFS.fs = new FS(thisFSPath);

			outputTB.Clear();
			outputTB.AppendText(helloText + NewLine);
			outputTB.AppendText(WorkWithFS.WhereAmI(null)[0]);

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
				WorkWithFS.fs = new FS(dialog.FileName);

				outputTB.Clear();
				outputTB.AppendText(helloText + NewLine);
				outputTB.AppendText(WorkWithFS.WhereAmI(null)[0]);
			}

		}

		private void форматироватьToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(thisFSPath))
			{
				MessageBox.Show("Сначала откройте файл с ФС");
				return;
			}

			WorkWithFS.fs.Close();

			var form = new FormFormatFS(thisFSPath);
			form.ShowDialog();

			WorkWithFS.fs.Open(thisFSPath);
		}

		private void фСToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}

		private void inputTB_KeyPress(object sender, KeyPressEventArgs e)
		{

		}

		private void PerformCommand(string strCommand)
		{
			Log(strCommand);

			var args = strCommand.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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

				outputTB.AppendText(WorkWithFS.WhereAmI(null)[0]);
			}

		}

		private void Log(string message)
		{
			outputTB.AppendText(message + NewLine);
		}

		private void Log(string[] messages)
		{
			foreach (var msg in messages)
			{
				Log(msg);
			}
		}

		public static class WorkWithFS
		{
			public static int thisDirIndex = 2;
			public static int thisUserId = 1;
			public static FS fs = null;

			/// <param name="_">Пустой массив параметров.</param>
			/// <returns>Краткая информация о каждом файле в текущей директории</returns>
			public static string[] GetFilesInCurrentDir(string[] _)
			{
				var table = new List<string>();
				string header = string.Format("{0,20} {1,5} {2,20} {3,10} {4,10} {5,7}", "Имя файла:", "Тип:", "Последнее изменение:", "Размер:", "Владелец:", "Его права:");

				table.Add(header);
				table.AddRange(fs.GetExistingFilesInDirInShortForm(thisDirIndex));

				return table.ToArray();
			}

			/// <param name="_">Пустой массив параметров.</param>
			/// <returns>Краткая информация о каждом пользователе</returns>
			public static string[] GetAllUsers(string[] _)
			{
				var table = new List<string>();
				String header = string.Format("{0,20} {1,20}", "Имя:", "Логин:");
				table.Add(header);
				table.AddRange(fs.GetAllExistingUsersStrings());
				return table.ToArray();
			}

			internal static string[] CreateDir(string[] args)
			{
				string fileName = args[0];
				var createFileResult = fs.CreateDir(thisDirIndex, fileName, thisUserId);
				string[] result = new string[1];

				switch (createFileResult)
				{
					case CreateFileResult.OK:
						result[0] = "Папка создана";
						break;
					case CreateFileResult.FileAlreadyExists:
						result[0] = "Ошибка. Такая папка уже существует";
						break;
					case CreateFileResult.NotEnoughRights:
						result[0] = "Ошибка. Недостаточно прав";
						break;
					case CreateFileResult.InvalidFileName:
						result[0] = "Ошибка. Недопустимое имя файла";
						break;
					case CreateFileResult.MaxFilesNumberReached:
						result[0] = "Ошибка. В системе достигнуто максимальное кол-во файлов";
						break;
					case CreateFileResult.PathTooLong:
						result[0] = "Ошибка. Создаваемый путь слишком длинный";
						break;
					default:
						result[0] = "Что-то пошло не так, и я даже не знаю что именно";
						break;
				}

				return result;
			}

			internal static string[] CreateFile(string[] args)
			{
				string fileName = args[0];


				var createFileResult = fs.CreateFile(thisDirIndex, fileName, FileType.Text, thisUserId);
				string[] result = new string[1];

				switch (createFileResult)
				{
					case CreateFileResult.OK:
						result[0] = "Файл создан";
						break;
					case CreateFileResult.FileAlreadyExists:
						result[0] = "Ошибка. Такой файл уже существует";
						break;
					case CreateFileResult.NotEnoughRights:
						result[0] = "Ошибка. Недостаточно прав";
						break;
					case CreateFileResult.InvalidFileName:
						result[0] = "Ошибка. Недопустимое имя файла";
						break;
					case CreateFileResult.MaxFilesNumberReached:
						result[0] = "Ошибка. В системе достигнуто максимальное кол-во файлов";
						break;
					default:
						result[0] = "Что-то пошло не так, и я даже не знаю что именно";
						break;
				}

				return result;
			}

			internal static string[] CreateUser(string[] args)
			{
				String name = args[0];
				String login = args[1];
				String password = args[2];
				var result = new string[1];

				var createUserResult = fs.AddUser(name, login, password);
				switch (createUserResult)
				{
					case CreateUserResult.OK:
						result[0] = "Готово";
						break;
					case CreateUserResult.MaxUsersCountReached:
						result[0] = "Ошибка. Уже создано максимальное кол-во пользователей";
						break;
					case CreateUserResult.UserAlreadyExists:
						result[0] = "Ошибка. Такой пользователь уже существует в системе";
						break;
					case CreateUserResult.OKButCanNotCreateUserDir:
						result[0] = "Пользователь создан, но папку создать не удалось";
						break;
					default:
						result[0] = "Что-то пошло не так, и я даже не знаю, что именно";
						break;
				}
				return result;
			}

			/// <summary>
			/// Переименовать файл в текущей папке.
			/// </summary>
			/// <returns>Результат переименования</returns>
			internal static string[] Rename(string[] args)
			{
				string oldName = args[0];
				string newName = args[1]; // если существует, вернуть это

				var result = new string[1];

				var renameResult = fs.RenameFileInDir(thisDirIndex, oldName, newName, thisUserId);

				switch (renameResult)
				{
					case CreateFileResult.OK:
						result[0] = "Готово";
						break;
					case CreateFileResult.FileAlreadyExists:
						result[0] = "Ошибка. Такой файл уже существует";
						break;
					case CreateFileResult.NotEnoughRights:
						result[0] = "Ошибка. Недостаточно прав";
						break;
					case CreateFileResult.InvalidFileName:
						result[0] = "Ошибка. Недопустимое имя файла";
						break;
					default:
						result[0] = "Что-то пошло не так, и я даже не знаю что именно";
						break;
				}


				return result;
			}

			internal static string[] GoTo(string[] args)
			{
				string path = args[0].ToNormalizedPath();

				var result = new string[1];

				string fullPathOfThisDir = fs.GetFullFilePathByMFTIndex(thisDirIndex).ToNormalizedPath();

				string tempDir = fullPathOfThisDir;

				// Так. Формат. 
				// Начало строки: 
				/*				~  или несколько ../   . Другие символы и эти штуки, но не в начале строки - 
				 */

				string destinationPath;
				if (path[0] == '~')
				{
					destinationPath = path.Replace("~", "$.");
				}
				else
				{ // Заменяем все ../ на нужный путь. И в итоге получаем путь, который хотели получить.
					destinationPath = AnalyzePath(path, originPath: fullPathOfThisDir);
				}

				// GetDirectoryName и GetFileName придется заменить своими методами. потому что Path возвращает хрень какую-то.
				int destinationDirIndex = fs.GetMFTIndexByPathAndFileName(fs.GetDirectoryName(destinationPath), fs.GetFileName(destinationPath));
				if (destinationDirIndex < 0)
				{
					result[0] = "Ошибка. Директория не найдена";
					return result;
				}

				if (fs.GetFileTypeByRecord(fs.GetMFTRecordByIndex(destinationDirIndex)) != FileType.Dir)
				{
					result[0] = "Ошибка. Заданный путь не ведет к директории";
					return result;
				}

				if (!fs.UserCanReadFile(destinationDirIndex, thisUserId))
				{
					result[0] = "Ошибка. Недостаточно прав на чтение директории.";
					return result;
				}

				thisDirIndex = destinationDirIndex;
				result[0] = "Готово";


				return result;

				string AnalyzePath(string pathToAnalizeOnBackSubstrings, string originPath)
				{
					originPath = originPath.ToNormalizedPath();
					var backSubstr = "../";

					// считаю кол-во вхождений в строке
					var count = pathToAnalizeOnBackSubstrings
								.Split(new string[] { backSubstr }, StringSplitOptions.None)
								.Count()
								- 1;
					for (var i = 0; i < count; i++)
					{
						originPath = fs.GetDirectoryName(originPath);
					}
					originPath = originPath.ToNormalizedPath();

					string destination = originPath + pathToAnalizeOnBackSubstrings.Replace(backSubstr, "");
					destination = new string(destination.Take(destination.Length - 1).ToArray());

					return destination;

					// Иду по строке, countBack = считаю "../" в начале. Получил любой другой символ - прекратил считать.
					// for(var i=0; i< countBack; i++){ originPath = Path.GetDirectoryName(originPath);}
					// string strToRemove = pathToAnalizeOnBackSubstrings.Substring(0, countBack * 3); // 3 is ../
					// var betterPath = pathToAnalizeOnBackSubstrings.Remove(strToRemove); 
					// var bestPath = originPath + betterPath;


				}
			}

			internal static string[] OpenFile(string[] args)
			{
				throw new NotImplementedException();
			}

			internal static string[] Login(string[] args)
			{
				string[] result = new string[1];
				string login = args[0].ToLower();
				string password = args[1];

				var user = fs.GetUserByLogin(login);
				if (user.Equals(default(UserRecord)))
				{
					result[0] = "Логин не найден";
					return result;
				}

				if (!user.PasswordHash.SequenceEqual(UserRecord.ComputeHash(password)))
				{
					result[0] = "Пароль неверен";
					return result;
				}

				thisUserId = user.User_id;

				result[0] = "Здравствуйте, " + user.Name.ToASCIIString();
				thisDirIndex = fs.UsersDirIndex;
				return result;
			}

			internal static string[] Logout(string[] args)
			{
				thisUserId = -1;
				return new string[] {"Готово"};
			}

			internal static string[] WhoAmI(string[] _)
			{
				var user = fs.GetUserById(thisUserId);
				if (user.Equals(default(UserRecord)))
				{
					return new String[] { "Сначала войдите в систему." };
				}

				String header = string.Format("{0,20} {1,20}", "Имя:", "Логин:");
				var result = new string[] { header, user.ToString() };
				return result;
			}

			internal static string[] WhereAmI(string[] _)
			{
				var record = fs.GetMFTRecordByIndex(thisDirIndex);
				string path = fs.GetFullFilePathByMFTRecord(record);
				path += "/>";

				return new[] { path };
			}
		}

		private void inputTB_KeyDown(object sender, KeyEventArgs e)
		{
			var tbox = sender as TextBox;
			if (e.KeyData == Keys.Enter)
			{
				var command = tbox.Text;
				PerformCommand(command);


				if (buffer.Count == 0 || buffer.Last() != tbox.Text)
				{
					buffer.Add(tbox.Text);
					bufIndex = buffer.Count - 1;
				}

				tbox.Clear();
				return;
			}

			if (e.KeyData == Keys.Up)
			{
				if (buffer.Count == 0)
					return;
				if (bufIndex > 0)
					bufIndex--;
				tbox.Text = buffer[bufIndex];
				return;
			}

			if (e.KeyData == Keys.Down)
			{
				if (buffer.Count == 0)
					return;
				if (bufIndex < buffer.Count - 1)
					bufIndex++;
				tbox.Text = buffer[bufIndex];
				return;
			}
		}

	}
}
