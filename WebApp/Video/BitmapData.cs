using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace WebApp.Video
{
	/// <summary>
	/// кдасс BitmapsData для работы с Bitmap
	/// </summary>
	public class BitmapsData
	{
		public Rectangle Rectangle1 { get; private set; }
		public PixelFormat Format { get; private set; }
		public Bitmap TempBitmap { get; set; }
		public ImageLockMode LockMode { get; private set; }
		public BitmapData BmpData { get; set; }
		public BitmapData BmpData2 { get; set; }
		public IntPtr Ptr { get; set; }
		public IntPtr Ptr2 { get; set; }
		public int NumBytes { get; private set; }

		public BitmapsData(int width, int height)
		{			
			Rectangle1 = new Rectangle(0, 0, width, height);
			Format = PixelFormat.Format24bppRgb;
			TempBitmap = new Bitmap(width, height, Format);
			LockMode = ImageLockMode.ReadWrite;
			BmpData = TempBitmap.LockBits(Rectangle1, LockMode, Format); 
			NumBytes = Math.Abs(BmpData.Stride) * height;
			TempBitmap.UnlockBits(BmpData);		
		}
	}
}
