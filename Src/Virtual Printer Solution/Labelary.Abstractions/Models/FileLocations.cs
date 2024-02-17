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
namespace Labelary.Abstractions
{
	public static class FileLocations
	{
		public static DirectoryInfo RootPath => new($@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\Virtual ZPL Printer");
		public static FileInfo ImageCache => new($@"{RootPath}\Image Cache");
		public static FileInfo Database => new($@"{RootPath}\Database\db.sqlite");
	}
}
