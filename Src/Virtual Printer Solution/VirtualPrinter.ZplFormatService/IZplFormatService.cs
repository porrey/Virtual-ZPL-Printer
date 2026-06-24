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
namespace VirtualPrinter.ZplFormatService
{
	public interface IZplFormatService
	{
		/// <summary>
		/// Directory where ^DF format bodies are persisted (simulated flash memory).
		/// </summary>
		DirectoryInfo FormatDirectory { get; }

		/// <summary>
		/// Detects ^DF commands in the ZPL and saves each format body to the flash
		/// library. 
		/// </summary>
		Task SaveFormatFromZplAsync(string zpl);

		/// <summary>
		/// Detects ^XF (recall format) commands in the ZPL, loads each stored
		/// template body, merges the ^FN field-number placeholders with the ^FD
		/// field-data values supplied in the print job, and replaces the ^XF
		/// command with the fully-populated template body.
		///
		/// Returns the original ZPL unchanged if no ^XF commands are found.
		/// </summary>
		Task<string> ApplyRecalledFormatsAsync(string zpl);
	}
}
