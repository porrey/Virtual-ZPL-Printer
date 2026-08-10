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
namespace VirtualPrinter.GrfStorageService
{
	public interface IGrfStorageService
	{
		/// <summary>
		/// Directory where ~DG blobs are persisted (simulated E: flash library).
		/// </summary>
		DirectoryInfo GrfDirectory { get; }

		/// <summary>
		/// Scans ZPL for ~DG commands and saves each blob to the flash library.
		/// Call this for every incoming ZPL job so that *ImageTemplate.zpl sends
		/// are automatically captured without any separate setup step.
		/// </summary>
		Task SaveGrfFromZplAsync(string zpl);

		/// <summary>
		/// Scans ZPL for ^XG references, looks each up in the flash library, and
		/// prepends the matching ~DG blob(s) into the payload so Labelary can
		/// resolve the graphic inline (Labelary has no persistent flash memory).
		/// Returns the original ZPL unchanged if no references are found or if
		/// no matching blobs are in the library.
		/// </summary>
		Task<string> ApplyReferencedGrfAsync(string zpl);

		/// <summary>
		/// Evicts a cached GRF blob so the next read reloads from disk.
		/// Call this after writing or deleting a GRF file outside of SaveGrfFromZplAsync.
		/// </summary>
		void InvalidateCache(string device, string filename);
	}
}
