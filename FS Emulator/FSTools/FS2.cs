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
	}
}
