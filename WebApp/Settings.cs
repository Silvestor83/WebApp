using System;
using System.IO;
using System.Xml.Serialization;

namespace WebApp
{
	public class Settings
	{
		private static volatile Settings data;
		private static readonly Object locker = new Object();
		private static readonly string settingsName = "Settings.xml";

		// Viseo Settings
		public string NameVideo = "WebCam Name";
		public bool IsRecordVideo = false;
		public int Time = 60;
		public int ResWidth = 640;
		public int ResHeight = 480;
		public int FrameRate = 25;
		public int BitRate = 2048000;
		public string PathVideo = @"C:\Video";
		// Audio Settings
		public string NameAudio = "Microphone Name";
		public bool IsRecordAudio = false;
		public int SampleRate = 44100;
		public int Chanels = 1;
		public string PathAudio = @"C:\Video\Audio";
		// Server settings
		public bool IsSendImages = false;
		public string Login = "Test";
		public string Password = "123456";
		public string Host = @"http://localhost:4602";
		public int ImagesPerMinute = 12;

		private Settings() { }

		public static Settings GetInstance()
		{
			if (data == null)
			{
				lock (locker)
				{
					if (data == null)
					{
						string path = AppDomain.CurrentDomain.BaseDirectory + settingsName;
						Serialization serialization = new Serialization(path);

						if (!File.Exists(path))
						{
							data = new Settings();
							serialization.Serialize(data);
						}
						else
						{
							data = serialization.Deserialize();
						}
					}
				}
			}
			return data;
		}
	}


	class Serialization
	{
		private readonly XmlSerializer serializer;
		private readonly string path;

		public Serialization(string path)
		{
			this.path = path;
			serializer = new XmlSerializer(typeof(Settings));
		}

		public void Serialize(Settings data)
		{
			using (Stream stream = File.Create(path))
			{
				serializer.Serialize(stream, data);
			}
		}

		public Settings Deserialize()
		{
			using (Stream stream = File.OpenRead(path))
			{
				Settings data = (Settings)serializer.Deserialize(stream);
				return data;
			}
		}
	}
}
