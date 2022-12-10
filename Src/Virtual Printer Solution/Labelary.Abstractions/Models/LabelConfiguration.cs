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
using System.Collections.Generic;
using UnitsNet.Units;

namespace Labelary.Abstractions
{
	public class LabelConfiguration : ILabelConfiguration
	{
		public int Dpmm { get; set; }
		public double LabelWidth { get; set; }
		public double LabelHeight { get; set; }
		public LengthUnit Unit { get; set; }
		public int LabelRotation { get; set; }
		public IEnumerable<ILabelFilter> LabelFilters { get; set; }
	}
}
