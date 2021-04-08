using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace ConsoleApp1
{
	public static class PrintDirect
	{
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern SafeFileHandle CreateFile(string lpFileName, FileAccess dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, FileMode dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

		public static Task<bool> SendBytesAsync(string port, byte[] data)
		{
			bool returnValue = false;

			SafeFileHandle fh = CreateFile(port, FileAccess.Write, 0, IntPtr.Zero, FileMode.OpenOrCreate, 0, IntPtr.Zero);

			if (!fh.IsInvalid)
			{
				using (FileStream io = new(fh, FileAccess.ReadWrite))
				{
					io.Write(data, 0, data.Length);
					io.Close();
				}

				returnValue = true;
			}

			return Task.FromResult(returnValue);
		}

		public static Task<bool> SendAsciiTextAsync(string port, string text)
		{
			byte[] buffer = Encoding.ASCII.GetBytes(text);
			return PrintDirect.SendBytesAsync(port, buffer);
		}

		public static Task<bool> SendUtf8TextAsync(string port, string text)
		{
			byte[] buffer = Encoding.UTF8.GetBytes(text);
			return PrintDirect.SendBytesAsync(port, buffer);
		}
	}
}