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
using VirtualPrinter.Models;

namespace VirtualPrinter
{
	public static class LabelTemplateExtensions
	{
		private static Random Rnd { get; } = new Random();

		public static string ApplyFieldValues(this LabelTemplate template)
		{
			return template.Zpl.ApplyFieldValues();
		}

		public static string ApplyFieldValues(this string zpl)
		{
			string returnValue = null;

			//
			// Create a random bar code value for the label.
			//
			int id1 = Rnd.Next(1, 9);
			int id2 = Rnd.Next(1, 99);
			int id3 = Rnd.Next(1, 999);
			int id4 = Rnd.Next(1, 9999);
			int id5 = Rnd.Next(1, 99999);
			int id6 = Rnd.Next(1, 999999);
			int id7 = Rnd.Next(1, 9999999);
			int id8 = Rnd.Next(1, 99999999);
			int id9 = Rnd.Next(1, 999999999);

			//
			// Replace template variables.
			//
			returnValue = zpl.Replace("{id1}", id1.ToString("0"))
							 .Replace("{id2}", id2.ToString("00"))
							 .Replace("{id3}", id3.ToString("000"))
							 .Replace("{id4}", id4.ToString("0000"))
							 .Replace("{id5}", id5.ToString("00000"))
							 .Replace("{id6}", id6.ToString("000000"))
							 .Replace("{id7}", id7.ToString("0000000"))
							 .Replace("{id8}", id8.ToString("00000000"))
							 .Replace("{id9}", id9.ToString("000000000"));

			return returnValue;
		}
	}
}
