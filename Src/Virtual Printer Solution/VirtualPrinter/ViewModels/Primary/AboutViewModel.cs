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
using System.Reflection;

namespace VirtualPrinter.ViewModels
{
	public class AboutViewModel
	{
		public string Title
		{
			get
			{
				return Properties.Strings.Main_Title;
			}
		}

		public string Version
		{
			get
			{
				Version version = Assembly.GetEntryAssembly().GetName().Version;
				return $"{Properties.Strings.About_Version} {version.Major}.{version.Minor}.{version.Build}";
			}
		}
	}
}
