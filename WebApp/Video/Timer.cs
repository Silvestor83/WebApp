using System;
using System.Runtime.InteropServices;

namespace WebApp.Video
{
	class Timer : IDisposable
	{
		[DllImport("winmm.dll")]
		private static extern int timeBeginPeriod(int msec);

		[DllImport("winmm.dll")]
		private static extern int timeEndPeriod(int msec);

		[DllImport("winmm.dll")]
		private static extern int timeSetEvent(int delay, int resolution, TimeProc proc, int user, int mode);

		[DllImport("winmm.dll")]
		private static extern int timeKillEvent(int id);

		delegate void TimeProc(uint id, uint msg, uint user, uint param1, uint param2);

		// Интервал работы таймера
		private readonly int interval;
		// Разрешение работы таймера. 0 - максимально возможное разрешение устанавливаемое системой
		private readonly int resolution = 0;
		private int mTimerId;
		private readonly TimeProc timeProc;
		private bool disposed = false;
		private readonly Action func;
		private int count = 0;

		public Timer(Action func, int frameRate)
		{
			this.func = func;
			interval = 1000 / frameRate;
			timeProc = TimerCallback;
		}

		private void TimerCallback(uint id, uint msg, uint user, uint param1, uint param2)
		{
			func();
			count++;
		}

		public void Start()
		{
			timeBeginPeriod(1);
			int timerMode = 1; // 1 for periodic, 0 for single event
			// Запуск таймера для выполнения делегата timeProc
			mTimerId = timeSetEvent(interval, resolution, timeProc, 0, timerMode);
			if (mTimerId == 0)
			{
				Console.WriteLine("Невозможно создать таймер");
			}
		}

		public void Stop()
		{
			Dispose();
			Console.WriteLine("количество тиков: " + count);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing) { /* Dispose managed resources */ }
				timeKillEvent(mTimerId);
				timeEndPeriod(1);
			}
			disposed = true;
		}

		~Timer()
		{
			Dispose(false);
		}
	}
}
