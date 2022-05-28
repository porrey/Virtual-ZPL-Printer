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
using System;
using VirtualZplPrinter.Models;

namespace VirtualZplPrinter
{
	public static class LabelTemplateExtensions
	{
		private static Random Rnd { get; } = new Random();

		public static string ApplyFieldValues(this LabelTemplate template)
		{
			string returnValue = null;

			//
			// Create a random bar code value for the label.
			//
			int id = LabelTemplateExtensions.Rnd.Next(1, 99999999);

			//
			// Read the sample ZPL.
			//
			returnValue = template.Zpl.Replace("{id}", id.ToString("00000000"));

			return returnValue;
		}
	}
}
