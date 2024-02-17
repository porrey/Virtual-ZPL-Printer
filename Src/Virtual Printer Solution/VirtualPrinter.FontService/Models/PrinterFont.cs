/*
 *  This file is part of Virtual ZPL Printer.
 *  
 *  Virtual ZPL Printer is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Virtual ZPL Printer is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Virtual ZPL Printer.  If not, see <https://www.gnu.org/licenses/>.
 */
using Newtonsoft.Json;

namespace VirtualPrinter.FontService
{
	internal class PrinterFont : IPrinterFont
	{
		public string BaseFileName { get; set; }
		public string FontName { get; set; }
		public string PrinterDevice { get; set; }
		public string Chars { get; set; }
		public int FontByteLength { get; set; }
		public string FontSource { get; set; }

		public static PrinterFont FromFile(FileInfo fileInfo)
		{
			PrinterFont returnValue = null;

			if (fileInfo.Exists)
			{
				string json = File.ReadAllText(fileInfo.FullName);
				returnValue = JsonConvert.DeserializeObject<PrinterFont>(json);
				returnValue.BaseFileName = Path.GetFileNameWithoutExtension(fileInfo.FullName);
			}

			return returnValue;
		}
	}
}