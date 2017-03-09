using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Text;
using AForge.Video.DirectShow;
using AForge.Video;
using AForge.Controls;
using AForge.Video.FFMPEG;
using WebApp.Network;


namespace WebApp.Video
{
	class VideoRec : IDisposable
	{
		/// Уникальный идентификатор устройств видеозахвата
		private readonly Guid guid;
		/// Список устройств видеозахвата
		private readonly FilterInfoCollection videoDevices;
		/// Устройство видеозахвата
		private readonly VideoCaptureDevice videoDevice;
		private readonly Timer timer;
		private readonly BitmapsData bD;
		private readonly Settings data;
		private readonly NetworkLauncher networkLauncher;
		/// Количество кадров полученных с устройства видеозахвата
		private int countFramesFromVideoDevice = 0;
		/// Количество кадров записанных в видеофайл
		private int countFramesToVideoFile = 0;
		//private AVIWriter writer;
		private readonly VideoFileWriter writerFfmpeg;
		private readonly ReaderWriterLockSlim blockRw;

		[DllImport("msvcrt.dll")]
		private static extern IntPtr memcpy(IntPtr dest, IntPtr src, int count);

		public VideoRec(string date)
		{
			data = Settings.GetInstance();

			//----- Инициализация устройства видеозахвата ----- 
			guid = FilterCategory.VideoInputDevice;
			videoDevices = new FilterInfoCollection(guid);
			videoDevice = GetVideoDevice();
			videoDevice.VideoResolution = GetVideoResolution();
			// Подписка на событие полученя кадра съемки с устройства видеозахвата
			videoDevice.NewFrame += CopyImage;

			//----- Инициализация класса записи видеофайла -----
			if (!Directory.Exists(data.PathVideo)) Directory.CreateDirectory(data.PathVideo);
			// Without compression
			/*writer = new AVIWriter("DIB ");
			writer.Open(data.pathVideo + @"\Web_" + date + ".avi", data.resWidth, data.resHeight);*/
			// With compression
			writerFfmpeg = new VideoFileWriter();
			writerFfmpeg.Open(data.PathVideo + @"\Web_" + date + ".avi", data.ResWidth, data.ResHeight, data.FrameRate, VideoCodec.H263P, data.BitRate);

			bD = new BitmapsData(data.ResWidth, data.ResHeight);
			timer = new Timer(Record, data.FrameRate);
			blockRw = new ReaderWriterLockSlim();
			if (data.IsSendImages)
			{
				networkLauncher = NetworkLauncher.GetInstance();
			}
		}

		/// <summary>
		/// Получение устройства видеозахвата
		/// </summary>
		private VideoCaptureDevice GetVideoDevice()
		{
			foreach (FilterInfo device in videoDevices)
			{
				if (device.Name.Contains(data.NameVideo))
				{
					return new VideoCaptureDevice(device.MonikerString);
				}
			}
			return null;
		}

		/// <summary>
		/// Установка разрешения видеозахвата
		/// </summary>
		private VideoCapabilities GetVideoResolution()
		{
			foreach (VideoCapabilities videocap in videoDevice.VideoCapabilities)
			{
				if (videocap.FrameSize.Width == data.ResWidth && videocap.FrameSize.Height == data.ResHeight)
				{
					return videocap;
				}
			}
			return null;
		}

		/// <summary>
		/// Получение информации о устройствах видеозахвата
		/// </summary>
		public void VideoInfo()
		{
			Console.WriteLine("Detected video devices:");
			foreach (FilterInfo device in videoDevices)
			{
				Console.WriteLine("Name of the device: " + device.Name);
				Console.WriteLine("MonikerString of the device: " + device.MonikerString);
			}

			Console.WriteLine("\nSelected video device: " + videoDevice.Source);
			Console.WriteLine("Is Running: " + videoDevice.IsRunning);
			Console.WriteLine("GUID of the device: " + guid);

			VideoCapabilities[] videocaps = videoDevice.VideoCapabilities;
			VideoCapabilities[] snapshotcaps = videoDevice.SnapshotCapabilities;

			Console.WriteLine();
			// Video capabilities of the device. If Length > 0, true
			Console.WriteLine("Number video capabilities of the device.: " + videocaps.Length);
			// Список поддерживаемых разрешений видеозахвата
			foreach (var videocap in videocaps)
			{
				var size = videocap.FrameSize.ToString();
				Console.WriteLine(size);
			}

			// Snapshot capabilities of the device. If Length > 0, true
			Console.WriteLine("Number snapshot capabilities of the device.: " + snapshotcaps.Length);
			// Список поддерживаемых разрешений фотозахвата
			foreach (var videocap in snapshotcaps)
			{
				var size = videocap.FrameSize.ToString();
				Console.WriteLine(size);
			}
		}

		/// <summary>
		/// Начало процесса видеозахвата
		/// </summary>
		public void Start()
		{
			videoDevice.Start();
			// Ждем пока устройство нормально запустится и затем запускаем таймер
			Thread.Sleep(1000);
			timer.Start();
		}

		/// <summary>
		/// Остановка процесса видеозахвата
		/// </summary>
		public void Stop()
		{
			Dispose();
			Console.WriteLine("Фреймов сделано: " + countFramesFromVideoDevice);
			Console.WriteLine("Фреймов записано: " + countFramesToVideoFile);
			countFramesFromVideoDevice = 0;
			countFramesToVideoFile = 0;
		}

		/// <summary>
		/// Копирование изображения с устройства видеозахвата во временное поле
		/// </summary>
		private void CopyImage(object sender, NewFrameEventArgs eventArgs)
		{
			countFramesFromVideoDevice++;
			if (blockRw.TryEnterWriteLock(200))
			{
				// Получить указатель на временное изображение
				bD.BmpData2 = bD.TempBitmap.LockBits(bD.Rectangle1, bD.LockMode, bD.Format);
				bD.Ptr2 = bD.BmpData2.Scan0;

				// Получить указатель на кадр полученный с устройства видеозахвата по событию
				bD.BmpData = eventArgs.Frame.LockBits(bD.Rectangle1, bD.LockMode, bD.Format);
				bD.Ptr = bD.BmpData.Scan0;

				// Копирование данных из полученного кадра во временный файл изображения
				memcpy(bD.Ptr2, bD.Ptr, bD.NumBytes);

				// Разблокировка изображений
				bD.TempBitmap.UnlockBits(bD.BmpData2);
				eventArgs.Frame.UnlockBits(bD.BmpData);

				blockRw.ExitWriteLock();
			}
			else
			{
				Console.WriteLine("Ошибка записи!");
			}
		}

		/// <summary>
		/// Метод записи изображений с устройства видеозахвата в видеофайл
		/// </summary>
		private void Record()
		{
			if (blockRw.TryEnterReadLock(200))
			{
				if (bD.TempBitmap != null)
				{
					if (writerFfmpeg != null && writerFfmpeg.IsOpen)
					{
						countFramesToVideoFile++;
						// Запись изображения в видеопоток
						try
						{
							writerFfmpeg.WriteVideoFrame(bD.TempBitmap);
						}
						catch (Exception e)
						{
							Console.WriteLine("writerFFMPEG открыт: " + writerFfmpeg.IsOpen);
							Console.WriteLine(e.Message);
						}

						// Отправка изображения на сервер
						if (networkLauncher != null && networkLauncher.IsLaunched)
						{
							// В соответствии с частотой видеозаписи определяется момент отправки кадра на сервер
							if ((data.ImagesPerMinute * countFramesToVideoFile) % (data.FrameRate * 60) == 0)
							{
								byte[] arr = GetBytesFromBitmap(bD.TempBitmap);
								networkLauncher.SendImage(arr);
							}
						}
					}
					else Console.WriteLine("Попытка записи в защищенную память!!!!!");

					// Ждать записи изображения в видеопоток и только затем снимать блокировку
					Thread.Sleep(5);
					blockRw.ExitReadLock();
				}
				else Console.WriteLine("Image == null");
			}
			else Console.WriteLine("Ошибка чтения!");
		}

		private byte[] GetBytesFromBitmap(Bitmap bitmap)
		{
			string date = DateTime.Now.ToString("yyyy-MM-dd  HH-mm-ss", CultureInfo.InvariantCulture);
			byte[] dateInBytes = Encoding.Unicode.GetBytes(date);
			MemoryStream ms = new MemoryStream();
			bitmap.Save(ms, ImageFormat.Jpeg);
			byte[] bitmapInBytes = ms.ToArray();
			int arrayLength = dateInBytes.Length + bitmapInBytes.Length;
			byte[] arrayLengthInBytes = BitConverter.GetBytes(arrayLength);
			byte[] sum = arrayLengthInBytes.Concat(dateInBytes.Concat(bitmapInBytes)).ToArray();
			return sum;
		}

		public void Dispose()
		{
			timer.Stop();
			// Ждем пока таймер остановится и только затем очищаем объект записи
			Thread.Sleep(1000);
			writerFfmpeg.Close();
			writerFfmpeg.Dispose();
			videoDevice.SignalToStop();
			videoDevice.WaitForStop();
			videoDevice.NewFrame -= CopyImage;
		}

		private void Error(object sender, VideoSourceErrorEventArgs eventArgs)
		{
			Console.WriteLine("Ошибка в воспроизведении файла");
		}

		/// <summary>
		/// Для снятия камшотов
		/// </summary>
		[Obsolete]
		public void RecordVideoFileFromSnapshotSource()
		{
			if ((videoDevice.SnapshotCapabilities != null) && (videoDevice.SnapshotCapabilities.Length != 0))
			{
				videoDevice.ProvideSnapshots = true;
				videoDevice.SnapshotResolution = videoDevice.SnapshotCapabilities.First();
				videoDevice.SnapshotFrame += new NewFrameEventHandler(CopyImage);
				videoDevice.VideoSourceError += new VideoSourceErrorEventHandler(Error);
				Console.WriteLine("Камера инициализарована");
			}

			VideoSourcePlayer videoSourcePlayer = new VideoSourcePlayer();
			videoSourcePlayer.VideoSource = videoDevice;

			Console.WriteLine("До старта");
			Console.WriteLine("Размер изображения: " + videoDevice.SnapshotResolution.FrameSize.ToString());

			videoDevice.Start();

			Console.WriteLine("После старта");
			Console.WriteLine("Размер изображения: " + videoDevice.SnapshotResolution.FrameSize.ToString());

			/*if ((videoDevice != null) && (videoDevice.ProvideSnapshots))
			{
				Console.WriteLine("Нажали кнопку");
				videoDevice.SimulateTrigger();
			}*/

			timer.Start();
			Thread.Sleep(10000);
			timer.Stop();
			Thread.Sleep(5000);
			Console.WriteLine("Количество фреймов: " + countFramesFromVideoDevice);
			videoDevice.SignalToStop();
			videoDevice.WaitForStop();
		}
	}
}
