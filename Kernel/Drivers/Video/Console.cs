using System;
using System.Runtime.InteropServices;

using Kernel.IO;

namespace Kernel.Drivers.Video;

public enum VideoMode 
{
	Serial,
	VGAText,
	VGAGraphics,
}

public enum ColorMode 
{
	B4F4, // background foreground
	R5G6B5,
	RGB565 = R5G6B5,
	R8G8B8,
	RGB = R8G8B8,
}

public static unsafe class Console 
{
	private static int x = 0;
	private static int y = 0;

	public static nint Framebuffer { set; get; } // physical address of video memory
	public static VideoMode VideoMode { set; get; } = VideoMode.Serial; // vesa gop vga intel nvidia ...
	public static ColorMode ColorMode { set; get; }

	public static int Width { set; get; } // width in chars not pixels
	public static int Height { set; get; } // height in chars not pixels
	public static int CharWidth { set; get; } // width of a char in pixels
	public static int CharHeight { set; get; } // height of a char in pixels
	public static int Pitch { set; get; } // bytes per scan line
	public static int Depth { set; get; } // bits per pixel

	public static (int, int) Size 
	{
		set => (Width, Height) = value;
		get => (Width, Height);
	}

	public static (int, int) CharSize 
	{
		set => (CharWidth, CharHeight) = value;
		get => (CharWidth, CharHeight);
	}

	public static (int, int) CursorPosition 
	{
		set => (x, y) = value;
		get => (x, y);
	}

	[Import] private static extern byte* getcharbitmap([MarshalAs(UnmanagedType.U2)] char x, int charWidth, int charHeight);

	private static Action<char, byte> GetCharPutMethod() 
	{
		switch (VideoMode) 
		{
			case VideoMode.VGAText:
				switch (ColorMode) 
				{
					case ColorMode.B4F4:
						return static (c, color) => ((ushort*)Framebuffer)[y * Width + x] = (ushort)((color << 8) | c);
					default:
						throw new NotImplementedException($"{VideoMode} with {ColorMode}.");
				}
			case VideoMode.VGAGraphics:
				switch (ColorMode) 
				{
					case ColorMode.R5G6B5:
						return static (c, color) => 
						{
							(int pixelw, int pixelh) = (Width * CharWidth, Height * CharHeight);

							// int* colors = stackalloc int[16] { 0x171421, 0x12488B, 0x26A269, 0x2AA1B3, 0xC01C28, 0xA347BA, 0xA2734C, 0xD0CFCC, 0x5E5C64, 0x2A7BDE, 0x33D17A, 0x33C7DE, 0xF66151, 0xC061CB, 0xE9AD0C, 0xFFFFFF };
							ushort* colors = stackalloc ushort[16] { 0x1084, 0x1230, 0x250c, 0x2cf5, 0xb8c4, 0x9a36, 0x9b89, 0xce78, 0x5acc, 0x2bda, 0x366e, 0x363a, 0xeae9, 0xbaf8, 0xe541, 0xffff };
							ushort* bgfg = stackalloc ushort[2] { colors[color >> 4], colors[color & 0xf] };
							// ushort* bgfg = stackalloc ushort[2] { 0x0000, 0xffff };

							byte* map = getcharbitmap(c, CharWidth, CharHeight);

							int i = 0;
							for (int dy = 0; dy < CharHeight; dy++)
								for (int dx = 0; dx < CharWidth; dx++) 
								{
									((ushort*)Framebuffer)[(y * CharHeight * pixelw) + (dy * pixelw) + (x * CharWidth) + dx] = bgfg[(map[i / CharWidth] >> (i % CharWidth)) & 1]; // CharWidth must be == sizeof(byte) * 8
									i++;
								}
						};
					case ColorMode.R8G8B8:
						return static (c, color) => 
						{
							(int pixelw, int pixelh) = (Width * CharWidth, Height * CharHeight);

							uint* colors = stackalloc uint[16] { 0x000000, 0x00008b, 0x008b00, 0x008b8b, 0x8b0000, 0x8b008b, 0x8b8b00, 0x8b8b8b, 0x5a5a5a, 0x0000ff, 0x00ff00, 0x00ffff, 0xff0000, 0xff00ff, 0xffff00, 0xffffff };
							uint* bgfg = stackalloc uint[2] { colors[color >> 4], colors[color & 0xf] };

							byte* map = getcharbitmap(c, CharWidth, CharHeight);

							int i = 0;
							for (int dy = 0; dy < CharHeight; dy++)
								for (int dx = 0; dx < CharWidth; dx++) 
								{
									int bpp = Depth / 8; // bytes per pixel not bits

									nint address = Framebuffer + ((y * CharHeight) * Pitch) + ((x * CharWidth) * bpp) + (dy * Pitch) + (dx * bpp);

									// CharWidth must be == sizeof(byte) * 8
									uint cc = bgfg[(map[i / CharWidth] >> (i % CharWidth)) & 1];

									// buffer is in BGR format ???
									((byte*)address)[0] = (byte)(cc >> 0);
									((byte*)address)[1] = (byte)(cc >> 8);
									((byte*)address)[2] = (byte)(cc >> 16);

									i++;
								}
						};
					default:
						throw new NotImplementedException($"{VideoMode} with {ColorMode}.");
				}
			case VideoMode.Serial:
				return static (c, color) => 
				{
					while ((IOPort.Read<byte>(0x3f8 + 5) & 0x20) == 0);
					IOPort.Write<byte>(0x3f8, (byte)c);
				};
			default:
				throw new NotImplementedException($"{VideoMode}");
		}
	}

	private static void Scroll(int lines) 
	{
		if (lines == 0)
			return;
		else if (lines > 0)
			throw new NotImplementedException("Cannot scroll up.");
		else if (lines < 0) 
		{
			int pitch = Pitch * CharHeight;
			lines = -lines;

			memcpy(Framebuffer, Framebuffer + (pitch * lines), (nuint)(pitch * (Height - lines)));
			memset(Framebuffer + (pitch * (Height - lines)), 0, (nuint)(pitch * lines));
		}

		[Import] static extern void memcpy(nint dest, nint src, nuint size);
		[Import] static extern void memset(nint dest, byte value, nuint count);
	}

	[Export("consoleclear")]
	private static void Clear() 
	{
		if (VideoMode == VideoMode.Serial)
			throw new ArgumentException($"Cannot clear '{nameof(VideoMode)}.{VideoMode}'.");

		Action<char, byte> put = GetCharPutMethod();

		int max = Width * Height;

		x = 0;
		y = 0;

		for (int i = 0; i < max; i++) 
		{
			if (x >= Width) 
			{
				x = 0;
				y++;
			}

			put(' ', 0);
			x++;
		}

		x = 0;
		y = 0;
	}

	[Export("consolewrite")]
	private static void WriteChars(char* chars, byte color) 
	{
		Action<char, byte> put = GetCharPutMethod();

		for (int i = 0; chars[i] != '\0'; i++) 
		{
			if (x >= Width) 
			{
				x = 0;
				y++;
			}

			if (VideoMode != VideoMode.Serial && y >= Height) 
			{
				Scroll(-1);
				y--;
			}

			switch (chars[i]) 
			{
				case '\r':
					x = 0;
					break;
				case '\n':
					if (VideoMode == VideoMode.Serial)
						put(chars[i], color);

					x = 0;
					y++;
					break;
				default:
					put(chars[i], color);
					x++;
					break;
			}
		}
	}
}
