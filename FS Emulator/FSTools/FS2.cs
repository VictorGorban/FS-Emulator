using FS_Emulator.FSTools.Structs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FS_Emulator.FSTools
{
	public partial class FS
	{
		private int List_BlocksFileIndex = 3;

		internal ModifyFileResult ModifyFile(int fileIndex, string newText, int userId)
		{
			// проверить fileIndex на exists
			// проверить fileIndex на UserCanWrite

			if (!GetIsFileExists(fileIndex))
			{
				return ModifyFileResult.FileNotExists;
			}
			if (!UserCanWriteFile(fileIndex, userId))
			{
				return ModifyFileResult.NotEnoughRights;
			}

			var newBytes = newText.ToBytes();
			if (IsFitsInMFT(newBytes))
			{
				var newDataBytes = newBytes.TrimOrExpandTo(MFTRecord.SpaceForData);
				bool wasNotInMFT = false;
				var blocks = new List<int>();

				var recordOffset = GetMFTRecordOffsetByIndex(fileIndex);
				// читаю wasNotInMFT. если true, считываю блоки и set them Free
				stream.Position = recordOffset + MFTRecord.OffsetForIsNotInMFT;
				var @byte = stream.ReadByte();
				if (@byte != 0)
					wasNotInMFT = true;
				if (wasNotInMFT)
				{
					stream.Position = recordOffset + MFTRecord.OffsetForData;
					var blocksToFree = GetMFTRecordDataFieldAsBlocksByIndex(fileIndex);
					SetBlocksFree(blocksToFree);
				}


				#region Запись в Data, set FileSize, SetInNotInMFT = false, set ModifyDate
				stream.Position = recordOffset + MFTRecord.OffsetForData;
				stream.Write(newDataBytes, 0, newDataBytes.Length);

				stream.Position = recordOffset + MFTRecord.OffsetForFileSize;
				stream.Write(BitConverter.GetBytes(newText.Length), 0, sizeof(int));

				stream.Position = recordOffset + MFTRecord.OffsetForIsNotInMFT;
				stream.Write(BitConverter.GetBytes(false), 0, sizeof(byte));

				stream.Position = recordOffset + MFTRecord.OffsetForTime_Modification;
				stream.Write(BitConverter.GetBytes(DateTime.Now.ToLong()), 0, sizeof(long));
				#endregion
				return ModifyFileResult.OK;
			}
			else// Если не вмещается в MFT
			{
				var newDataBytes = newBytes;
				var blockSize = GetBlockSizeInBytes();
				int reqNumberOfBlocks = newDataBytes.Length / blockSize;
				if (newDataBytes.Length % blockSize != 0)
					reqNumberOfBlocks++;
				bool wasNotInMFT = false;
				var blocks = new List<int>();

				var recordOffset = GetMFTRecordOffsetByIndex(fileIndex);
				// читаю wasNotInMFT. если true, считываю блоки и set them Free
				stream.Position = recordOffset + MFTRecord.OffsetForIsNotInMFT;
				var @byte = stream.ReadByte();
				if (@byte != 0)
					wasNotInMFT = true;
				if (wasNotInMFT)
				{ // получаем блоки, где были данные и анализируем кол-во
					stream.Position = recordOffset + MFTRecord.OffsetForData;
					var blocksWere = GetMFTRecordDataFieldAsBlocksByIndex(fileIndex);
					//blocks.AddRange(blocksWere);

					if (blocksWere.Count > reqNumberOfBlocks)
					{
						SetBlocksFree(blocksWere.Skip(reqNumberOfBlocks));
						blocks.AddRange(blocksWere.Take(reqNumberOfBlocks));
					}
					else if (blocksWere.Count < reqNumberOfBlocks)
					{
						blocks.AddRange(blocksWere);
						var extraBlocks = GetFreeBlocks(reqNumberOfBlocks - blocksWere.Count);
						if (extraBlocks.Count() < reqNumberOfBlocks - blocksWere.Count)
							return ModifyFileResult.NotEnoughSpace;

						blocks.AddRange(extraBlocks);
						SetBlocksBusy(extraBlocks);
					}
				}

				if (blocks.Count < reqNumberOfBlocks)
				{
					var extraBlocks = GetFreeBlocks(reqNumberOfBlocks - blocks.Count);
					if (extraBlocks.Count() < reqNumberOfBlocks - blocks.Count)
						return ModifyFileResult.NotEnoughSpace;

					blocks.AddRange(extraBlocks);
					SetBlocksBusy(extraBlocks);
				}

				var blocksBytes = new List<byte>();
				foreach (var block in blocks)
				{
					blocksBytes.AddRange(BitConverter.GetBytes(block));
				}

				#region Запись блоков в Data, set FileSize, SetInNotInMFT = true, запись по блокам
				stream.Position = recordOffset + MFTRecord.OffsetForData;
				stream.Write(blocksBytes.ToArray().TrimOrExpandTo(MFTRecord.SpaceForData), 0, MFTRecord.SpaceForData);

				stream.Position = recordOffset + MFTRecord.OffsetForFileSize;
				stream.Write(BitConverter.GetBytes(newDataBytes.Length), 0, sizeof(int));

				stream.Position = recordOffset + MFTRecord.OffsetForIsNotInMFT;
				stream.Write(BitConverter.GetBytes(true), 0, sizeof(byte));

				stream.Position = recordOffset + MFTRecord.OffsetForTime_Modification;
				stream.Write(BitConverter.GetBytes(DateTime.Now.ToLong()), 0, sizeof(long));

				#region Запись newDataBytes по блокам.
				using (var ms = new MemoryStream(newDataBytes))
				{
					foreach (var block in blocks)
					{
						var blockBuffer = new byte[blockSize];
						ms.Read(blockBuffer, 0, blockBuffer.Length); // заполняю буфер куском новых данных

						stream.Position = block * blockSize;
						stream.Write(blockBuffer, 0, blockBuffer.Length); // пишу по адресу блока нужный кусок данных
					}
				}
				#endregion

				#endregion
				return ModifyFileResult.OK;
			}


			/*Само изменение:
			 * Если вмещается в MFT, то просто заменить Data и FileSize, и еще записать что IsInMFT. Если файл был не в MFT, то сначала освободить блоки из Data
			 * Если не вмещается в MFT:
			 * 
			 * Получить кол-во нужных блоков для записи
			 * Получить запись. Из нее получить IsNotInMFT
			 * 
			 * Если IsNotInMFT, то Data понимаем как блоки и считываем их номера (blocks)
			 * Если InMFT, то blocks = new List<int>
			 * 
			 * Сравниваем blocks.Count с кол-вом необходимых блоков
			 * Если нужно меньше, то list dif = blocks.Skip(countNeededBlocks); blocksToWrite = blocks.Take(countNeededBlocks); SetFreeBlocks(dif);
			 * Если нужно больше, то list dif = GetFreeBlocks(countNeeded - blocks.Length); blocksToWrite = blocks.AppendRange(dif);  SetBusyBlocks(dif);
			 * (Можно вернуть NotEnoughSpace)
			 * Считаем Offset для блоков
			 * Запись данных по блокам (memoryStream(newData).Read(blockSize) -> stream.Write(blockOffset))
			 * // Последний блок: Расширить до blockSize, чтобы считывать можно было без особых проблем.
			 * SetFileSize
			 * return OK;
			 */

			// Самое время при создании FS SetBusyBlocks(все блоки до Data). Т.к. времени мало, то это == foreach(...) setBusyBlock(block);

			throw new NotImplementedException();
		}

		private void SetBlocksBusy(IEnumerable<int> blocksToSetBusy)
		{
			foreach (var block in blocksToSetBusy)
			{
				SetBlockBusy(block);
			}

			DecreaseFreeBlocksCount(blocksToSetBusy.Count());
		}

		private void DecreaseFreeBlocksCount(int count)
		{
			var startPosition = 0 + ServiceRecord.OffsetForNumber_Of_Free_Blocks;
			var buf = new byte[8];
			stream.Position = startPosition;
			stream.Read(buf, 0, buf.Length);

			long oldCount = BitConverter.ToInt64(buf, 0);
			var newCount = oldCount - count;
			buf = BitConverter.GetBytes(newCount);

			stream.Position = startPosition;
			stream.Write(buf, 0, buf.Length);
		}

		private void SetBlockBusy(int block)
		{
			var offsetInBytes = block / 8;
			var shift = block % 8;

			int fileOffset = GetByteStartList_Blocks();
			stream.Position = fileOffset + offsetInBytes;

			byte byteToModify = (byte)stream.ReadByte();
			byte modifiedByte = SetBitBusy(byteToModify, shift);

			stream.Position = fileOffset + offsetInBytes;
			stream.WriteByte(modifiedByte);
		}

		private byte SetBitBusy(byte byteToAnalize, int shift)
		{
			var bits = new MyBitArray(byteToAnalize);
			bits[shift] = true;
			return bits.ToByte();
		}

		private IEnumerable<int> GetFreeBlocks(int reqBlocksNum)
		{
			var blocks = new List<int>();
			if (GetNumber_Of_Free_Blocks() < reqBlocksNum)
				return blocks;

			int fileSize = GetFileSize(List_BlocksFileIndex);

			int offset = GetByteStartList_Blocks();
			offset += GetBlockStartData();
			int startByte = offset;
			int endByte = offset + fileSize;
			int readBytes = 0;

			stream.Position = startByte;
			while (stream.Position < endByte && blocks.Count < reqBlocksNum)
			{
				byte readByte = (byte)stream.ReadByte();
				if (readByte != 0b11_11_11_11)
				{
					var freeBitsInThisByte = GetFreeBitsNumbers(readByte);
					foreach (var bitNumber in freeBitsInThisByte)
					{
						if (blocks.Count == reqBlocksNum)
							return blocks;
						else
							blocks.Add(startByte + readByte * 8 + bitNumber);
					}
				}
				readBytes++;
			}


			return blocks;

			List<int> GetFreeBitsNumbers(byte byteToAnalyze)
			{
				var list = new List<int>();

				var bits = new MyBitArray(byteToAnalyze);
				for (var i = 0; i < bits.Length; i++)
				{
					if (bits[i] == false)
						list.Add(i);
				}

				return list;
			}
		}

		private void SetBlocksFree(IEnumerable<int> blocksToSetFree)
		{
			foreach (var block in blocksToSetFree)
			{
				SetBlockFree(block);
			}
			IncreaseFreeBlocksCount(blocksToSetFree.Count());
		}

		private void IncreaseFreeBlocksCount(int count)
		{
			var startPosition = 0 + ServiceRecord.OffsetForNumber_Of_Free_Blocks;
			var buf = new byte[8];
			stream.Position = startPosition;
			stream.Read(buf, 0, buf.Length);

			long oldCount = BitConverter.ToInt64(buf, 0);
			var newCount = oldCount + count;
			buf = BitConverter.GetBytes(newCount);

			stream.Position = startPosition;
			stream.Write(buf, 0, buf.Length);
		}

		private void SetBlockFree(int block)
		{
			var offsetInBytes = block / 8;
			var extraBits = block % 8;

			int fileOffset = GetByteStartList_Blocks();
			stream.Position = fileOffset + offsetInBytes;

			byte byteToModify = (byte)stream.ReadByte();
			byte modifiedByte = SetBitFree(byteToModify, extraBits);

			stream.Position = fileOffset + offsetInBytes;
			stream.WriteByte(modifiedByte);
		}

		private int GetByteStartList_Blocks()
		{
			var blocks = GetMFTRecordDataFieldAsBlocksByIndex(List_BlocksFileIndex);
			int startBlock = blocks[0];

			return startBlock * GetBlockSizeInBytes();
		}

		private byte SetBitFree(byte byteToAnalize, int shift)
		{
			var bits = new MyBitArray(byteToAnalize);
			bits[shift] = false;
			return bits.ToByte();
		}

		private List<int> GetMFTRecordDataFieldAsBlocksByIndex(int fileIndex)
		{
			return GetMFTRecordDataFieldAsBlocksByRecord(GetMFTRecordByIndex(fileIndex));
		}

		private List<int> GetMFTRecordDataFieldAsBlocksByRecord(byte[] record)
		{
			var blocks = new List<int>();
			byte[] data = new byte[MFTRecord.SpaceForData];
			using (var ms = new MemoryStream(record))
			{
				ms.Position = MFTRecord.OffsetForData;
				// считать все это как блоки, потом удалить из списка нулевые.
				ms.Read(data, 0, data.Length);
			}
			using (var dataMS = new MemoryStream(data))
			{
				while (dataMS.Position < data.Length)
				{
					var buf = new byte[4];
					dataMS.Read(buf, 0, buf.Length);
					var block = BitConverter.ToInt32(buf, 0);
					if (block == 0) // все, дошли до конца. Нулевого блока не может быть.
						break;
					else
						blocks.Add(block);
				}
			}


			return blocks;
		}

		private bool IsFitsInMFT(byte[] bytes)
		{
			return bytes.Length <= MFTRecord.SpaceForData;
		}

		private bool GetIsFileExists(int fileIndex)
		{
			return GetIsFileExists(GetMFTRecordByIndex(fileIndex));
		}

		public void Move(int fileIndex, int originDirIndex, int destDirIndex)
		{
			/*Да ну нафиг, если есть такое имя папки/файла - return FileAlreadyExists. 
			 * Да, и для всех файлов заменить path, на новый путь (GetFullFileName(destDirIndex)).*/
			/*Для файла (или для всех файлов, если это директория) заменить path на fullFileName(destDirIndex)*/
			// я подобную рекурсию в удалении делал. В удалении юзера - особенно.
			var oldPathPart = GetFilePath(fileIndex);
			var newPathPart = GetFullFilePathByMFTIndex(destDirIndex);/*.ToNormalizedPath();*/

			// Для всех файлов в директории: 
			// Если это файл, то ReplacePathPart(replacedPath, partToReplaceOn); Если это Dir, то Для всех файлов в директории...

			#region Изменяю path для папки и всех файлов в ней
			ReplacePathPartOfForFileOrDir(fileIndex, oldPathPart, newPathPart);
			#endregion

			#region Удаляю запись о файле в originDir
			var startPositionForOriginDir = GetMFTRecordOffsetByIndex(originDirIndex);
			startPositionForOriginDir += MFTRecord.OffsetForData;
			var fileSizeOfOriginDir = GetFileSize(originDirIndex);
			var endPositionForOriginDir = startPositionForOriginDir + fileSizeOfOriginDir;

			stream.Position = startPositionForOriginDir;
			while (stream.Position < endPositionForOriginDir)
			{
				// блин, опять читаются нули
				var headerBuf = new byte[FileHeader.SizeInBytes];
				stream.Read(headerBuf, 0, headerBuf.Length);

				var header = FileHeader.FromBytes(headerBuf);
				if (header.IndexInMFT == fileIndex)
				{
					header.IndexInMFT = 0;

					// смещаемся в потоке назад на 1 header, записываем новый header
					stream.Position -= headerBuf.Length;
					stream.Write(header.ToBytes(), 0, FileHeader.SizeInBytes);

					break;
				}
			}
			#endregion

			#region Добавляю запись о файле в DestDir
			AddFileHeaderToDir(destDirIndex, fileIndex);
			#endregion

		}

		public void ReplacePathPartOfForFileOrDir(int fileIndex, string oldPart, string newPart)
		{
			var fileType = GetFileTypeByIndex(fileIndex);
			if (fileType == FileType.Dir)
			{
				var headers = GetFileHeadersOfExistingFilesByDirIndex(fileIndex);
				foreach (var header in headers)
				{
					var index = header.IndexInMFT;
					ReplacePathPartOfForFileOrDir(index, oldPart, newPart);
				}
			}

			// Получаю смещение; Получаю путь; Получаю обновленный путь; Записываю новый путь

			var offset = GetMFTRecordOffsetByIndex(fileIndex);
			offset += MFTRecord.OffsetForPath;
			byte[] oldPathBytes = new byte[MFTRecord.LengthOfPath];

			stream.Position = offset;
			stream.Read(oldPathBytes, 0, MFTRecord.LengthOfPath);

			string oldPath = oldPathBytes.ToASCIIString();
			string newPath = oldPath.Replace(oldPart, newPart);
			byte[] newPathBytes = newPath.ToBytes().TrimOrExpandTo(MFTRecord.LengthOfPath);

			stream.Position = offset;
			stream.Write(newPathBytes, 0, MFTRecord.LengthOfPath);
		}

		private string GetFilePath(int fileIndex)
		{
			return GetFilePath(GetFullFilePathByMFTIndex(fileIndex));
		}

		/// <summary>
		/// Удаляет файл. Без проверок.
		/// </summary>
		/// <param name="parentDirIndex">Индекс директории с удаляемым файлом</param>
		/// <param name="fileIndex">MFT индекс файла, который надо удалить</param>
		public void RemoveFile(int parentDirIndex, int fileIndex)
		{
			// Удалить запись из MFT 
			// Освободить блоки, если есть
			// Удалить запись из директории

			#region Удаление из MFT
			var mftRecOffset = GetMFTRecordOffsetByIndex(fileIndex);
			var fileExistsOffset = mftRecOffset + MFTRecord.OffsetForIsFileExists;

			stream.Position = fileExistsOffset;
			stream.WriteByte(0); // записываем туда false
			#endregion

			#region Освобождение занятых файлом блоком, если такие есть
			var isNotInMFTOffset = mftRecOffset + MFTRecord.OffsetForIsNotInMFT;

			stream.Position = isNotInMFTOffset;
			if (stream.ReadByte() != 0) // т.е isNotInMFT == true
			{
				var blocks = GetMFTRecordDataFieldAsBlocksByIndex(fileIndex);
				SetBlocksFree(blocks);
			}
			#endregion

			#region Удаление записи из файла директории
			var mftDirRecOffset = GetMFTRecordOffsetByIndex(parentDirIndex);
			//var d = GetFileDataByMFTIndex(parentDirIndex);
			var dirFileSizeOffset = mftDirRecOffset + MFTRecord.OffsetForFileSize;
			var dirDataOffset = mftDirRecOffset + MFTRecord.OffsetForData;
			#region Поиск записи в директории и set it as deleted





			#region Считывание FileSize
			var intBuf = new byte[4];
			int fileSize;

			stream.Position = dirFileSizeOffset;
			stream.Read(intBuf, 0, intBuf.Length);
			fileSize = BitConverter.ToInt32(intBuf, 0);
			#endregion

			byte[] d = new byte[fileSize];
			stream.Position = dirDataOffset; // чуствую, тут что-то не так.
			stream.Read(d, 0, d.Length); // хмм, все нули...




			var startPosition = dirDataOffset;
			var endPosition = startPosition + fileSize;

			stream.Position = startPosition;
			while (stream.Position < endPosition)
			{
				var headerBuf = new byte[FileHeader.SizeInBytes];
				stream.Read(headerBuf, 0, headerBuf.Length);

				var header = FileHeader.FromBytes(headerBuf);
				if (header.IndexInMFT == fileIndex)
				{
					header.IndexInMFT = 0;

					// смещаемся в потоке назад на 1 header, записываем новый header
					stream.Position -= headerBuf.Length;
					stream.Write(header.ToBytes(), 0, FileHeader.SizeInBytes);

					break;
				}
			}
			#endregion
			#endregion
			DecreaseFilesCount();
		}

		public void RemoveDir(int parentDirIndex, int dirIndex)
		{
			// здесь будет foreach по headers, рекурсия по if dir, а для остальных это RemoveFile

			// foreach по dirIndex.headers.
			// если header == dir, RemoveDir (dirIndex), 
			// если header == anyfile, потом removeFile(dirIndex).
			// В конце Dir тоже removeFile

			var headers = GetFileHeadersOfExistingFilesByDirIndex(dirIndex);
			foreach (var header in headers)
			{
				var index = header.IndexInMFT;
				if (GetFileTypeByIndex(index) == FileType.Dir)
					RemoveDir(dirIndex, index);
				else
					RemoveFile(dirIndex, index);
			}

			RemoveFile(parentDirIndex, dirIndex);
		}

		internal void RemoveUser(short userId)
		{
			// 1) собственно удаление пользователя
			// 2) Проход по всем файлам ФС. Если их владелец был userId, то сменить его на 0 (rootUserId)

			#region Само удаление пользователя
			if (userId < 2)
			{
				throw new ArgumentException("Нельзя удалять этого пользователя.", nameof(userId));
			}

			int startFilePosition = GetByteStartUsers();
			int userRecordOffset = startFilePosition + userId * UserRecord.SizeInBytes;
			int offsetForUserExists = userRecordOffset + UserRecord.OffsetForIsUserExists;

			stream.Position = offsetForUserExists;
			stream.WriteByte(0); // false

			#endregion


			#region Change fileOwner если бывший владелец - удаленный
			ChangeDirOwnerFromOldToNewOwner(0, userId, RootUserId);

			#endregion

		}

		/// <summary>
		/// Сменяет владельца папки и всех файлов в папке, но только если старый владелец файла - указанный
		/// </summary>
		/// <param name="dirIndex">Папка, владельца которой нужно сменить</param>
		/// <param name="oldUserId">Старый владелец</param>
		/// <param name="newUserId">Новый владелец</param>
		public void ChangeDirOwnerFromOldToNewOwner(int dirIndex, short oldUserId, short newUserId)
		{
			var headers = GetFileHeadersOfExistingFilesByDirIndex(dirIndex);
			foreach (var header in headers)
			{
				var index = header.IndexInMFT;
				if (GetFileTypeByIndex(index) == FileType.Dir)
					ChangeDirOwnerFromOldToNewOwner(index, oldUserId, newUserId);
				else
					ChangeFileOwnerFromOldToNewOwner(index, oldUserId, newUserId);
			}

			ChangeFileOwnerFromOldToNewOwner(dirIndex, oldUserId, newUserId);
		}

		public void ChangeFileOwnerFromOldToNewOwner(int fileIndex, short oldUserId, short newUserId)
		{
			var offset = GetMFTRecordOffsetByIndex(fileIndex);
			offset += MFTRecord.OffsetForOwnerRights;
			offset += UserRights.OffsetForUserId;

			var userIdBuf = new byte[2];
			stream.Position = offset;
			stream.Read(userIdBuf, 0, sizeof(short));

			if (BitConverter.ToInt16(userIdBuf, 0) == oldUserId)  // if userId у файла == oldUserId
			{
				stream.Position = offset;
				stream.Write(BitConverter.GetBytes(newUserId), 0, sizeof(short));
			}
		}

		public FileType GetFileTypeByIndex(int index)
		{
			return GetFileTypeByRecord(GetMFTRecordByIndex(index));
		}
	}
}
