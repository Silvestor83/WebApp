using System;
using System.Threading;
using WebApp.Audio;
using WebApp.Video;

namespace WebApp
{
	/// <summary>
	/// Класс непрерывной записи аудио и видео через заданные промежутки времени
	/// </summary>
	class Rec
	{
		private AudioRec audioRec;
		private VideoRec webCam;
		private readonly Settings data;		

		public Rec()
		{
			data = Settings.GetInstance();
		}

		private void Initialize()
		{
			string date = DateTime.Now.ToString("yyyy-MM-dd  HH-mm-ss");
			if (data.IsRecordAudio) audioRec = new AudioRec(date);
			if (data.IsRecordVideo) webCam = new VideoRec(date);
		}

		/// <summary>
		/// Начало записи аудио и видео с устройства
		/// </summary>
		private void Start()
		{
			if (audioRec != null) audioRec.Start();
			if (webCam != null) webCam.Start();
		}

		/// <summary>
		/// Конец записи аудио и видео с устройства
		/// </summary>
		private void Stop()
		{
			if (audioRec != null) audioRec.Stop();
			if (webCam != null) webCam.Stop();
		}

		/// <summary>
		/// Основной цикл программы
		/// </summary>
		public void Run()
		{
			do
			{
				Initialize();
				Start();
				Thread.Sleep(data.Time * 1000);
				Stop();
			} while (true);
		}
	}
}
