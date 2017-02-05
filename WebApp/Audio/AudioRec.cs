using System;
using System.IO;
using NAudio.Wave;

namespace WebApp.Audio
{
	class AudioRec : IDisposable
	{
		private WaveInEvent waveIn;
		private WaveFileWriter writer;
		private readonly Settings data;

		public AudioRec(string date)
		{
			data = Settings.GetInstance();
			waveIn = new WaveInEvent
			{
				WaveFormat = new WaveFormat(data.SampleRate, data.Chanels),
				DeviceNumber = GetAudioDevice()
			};
			waveIn.DataAvailable += WriteAudio;
			if (!Directory.Exists(data.PathAudio)) Directory.CreateDirectory(data.PathAudio);
			writer = new WaveFileWriter(data.PathAudio + @"\Audio_" + date + ".wav", waveIn.WaveFormat);
		}

		private int GetAudioDevice()
		{
			int numberAudioDevices = WaveInEvent.DeviceCount;
			for (int i = 0; i < numberAudioDevices; i++)
			{
				if (WaveInEvent.GetCapabilities(i).ProductName.Contains(data.NameAudio))
				{
					return i;
				}
			}
			// ToDo Выбирается первое из доступных устройств, если не найдено соответствия по имени.
			return 0;
		}

		/// <summary>
		/// получение информации о доступных аудиозаписывающих устройствах
		/// </summary>
		public void AudioInfo()
		{			
			int numberAudio = WaveInEvent.DeviceCount;
			Console.WriteLine("Количество аудиозаписывающих устройств в системе: " + numberAudio);
			for (int i = 0; i < numberAudio; i++)
			{
				WaveInCapabilities audioCapabilities = WaveInEvent.GetCapabilities(i);
				Console.WriteLine("Имя устройства: " + audioCapabilities.ProductName);
				Console.WriteLine("GUID: " + audioCapabilities.ProductGuid);
				Console.WriteLine("Количество поддерживаемых каналов: " + audioCapabilities.Channels);
				Console.WriteLine();
			}			
		}
		
		public void Start()
		{
			waveIn.StartRecording();
		}

		public void Stop()
		{
			if (waveIn != null) waveIn.StopRecording();
			Dispose();
		}

		private void WriteAudio(object sender, WaveInEventArgs e)
		{
			writer.Write(e.Buffer, 0, e.BytesRecorded);
		}

		public void Dispose()
		{
			if (waveIn != null)
			{
				waveIn.Dispose();
				waveIn.DataAvailable -= WriteAudio;
				waveIn = null;
			}
			if (writer != null)
			{
				writer.Dispose();
				writer = null;
			}
		}
	}
}
