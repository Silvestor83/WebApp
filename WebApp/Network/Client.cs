using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using WebApp;

namespace WebApp.Network
{
	//interface ISendable
	//{
	//	void SendImage(byte[] arr);
	//	bool SuccessSend { get; set; }
	//	bool ServerError { get; set; }
	//}
	/// <summary>
	/// Класс для отправки данных по сети через TCP протокол
	/// </summary>
	class TcpDataSender : IDisposable
	{
		private readonly TcpClient client;
		private readonly NetworkStream stream;
		private AutoResetEvent autoResetEvent;
		private byte[] arrBytes;
		private bool successSend;
		public bool ServerError { get; set; }

		public TcpDataSender(int portNumber)
		{
			try
			{
				client = new TcpClient("localhost", portNumber);
				stream = client.GetStream();
			}
			catch (Exception e)
			{
				throw;
			}

			new Thread(Send).Start();
			autoResetEvent = new AutoResetEvent(false);
			successSend = true;
			ServerError = false;
		}

		private void Send()
		{
			try
			{
				while (true)
				{
					autoResetEvent.WaitOne();

					BinaryWriter w = new BinaryWriter(stream);
					w.Write(arrBytes);
					w.Flush();
					successSend = true;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				ServerError = true;
			}

		}

		public void SendImage(byte[] arr)
		{
			if (successSend)
			{
				arrBytes = (byte[])arr.Clone();
				successSend = false;
				autoResetEvent.Set();
			}
		}

		public void Dispose()
		{
			stream.Close();
			client.Close();
		}
	}


	/// <summary>
	/// Класс для установки соединения соединения с сервером через http и получения порта для последующего 
	/// взаимодействия по Tcp
	/// </summary>
	class Connection
	{
		private readonly Settings settings;
		private readonly WebClient client;

		public Connection()
		{
			settings = Settings.GetInstance();
			client = new WebClient { Proxy = null };
			client.QueryString.Add("Login", settings.Login);
			client.QueryString.Add("Password", settings.Password);
		}

		public int GetPortNumber()
		{
			string path = "/Application/LaunchTcpServer";
			try
			{
				client.DownloadString(settings.Host + path);
			}
			catch (WebException e)
			{
				var response = (HttpWebResponse)e.Response;
				if (response.StatusCode == HttpStatusCode.Unauthorized)
				{
					Console.WriteLine("Пользователь с указанными регистрационными данными не найден, либо пароль не верен.");
				}
				else if (response.StatusCode == HttpStatusCode.InternalServerError)
				{
					Console.WriteLine("Не удалось обработать запрос или запустить Tcp сервер.");
				}
				else Console.WriteLine("Во время запроса произошла ошибка.");
				throw;
			}

			string port = client.ResponseHeaders["PortNumber"];
			return int.Parse(port);
		}
	}


	/// <summary>
	/// Класс для запуска сетевого взаимодействия и поддержание его работающим в случае ошибок сети
	/// либо ошибок сервера
	/// </summary>
	class NetworkLauncher : IDisposable
	{
		private static volatile NetworkLauncher instance;
		private static readonly object lockObj = new object();
		private TcpDataSender dataSender;
		private Thread runThread;
		public bool IsLaunched { get; set; }

		private NetworkLauncher()
		{
			Run();
		}

		public static NetworkLauncher GetInstance()
		{
			if (instance == null)
			{
				lock (lockObj)
				{
					if (instance == null)
					{
						instance = new NetworkLauncher();
					}
				}
			}
			return instance;
		}

		private void Run()
		{
			runThread = new Thread(LaunchServer);
			runThread.Start();
		}

		private void LaunchServer()
		{
			while (!IsLaunched)
			{
				try
				{
					Connection connection = new Connection();
					var port = connection.GetPortNumber();
					dataSender = new TcpDataSender(port);
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
					Thread.Sleep(60000);
					continue;
				}
				IsLaunched = true;
			} 
		}

		public void SendImage(byte[] arr)
		{
			if (dataSender.ServerError)
			{
				IsLaunched = false;
				dataSender.Dispose();
				Run();
			}
			else dataSender.SendImage(arr);
		}

		public void Dispose()
		{
			dataSender.Dispose();
		}
	}
}