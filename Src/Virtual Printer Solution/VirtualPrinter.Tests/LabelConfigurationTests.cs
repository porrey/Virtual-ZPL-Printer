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
using Labelary.Abstractions;
using UnitsNet.Units;
using Xunit;

namespace VirtualPrinter.Tests
{
	public class LabelConfigurationTests
	{
		[Fact]
		public void Properties_SetAndGet_CorrectValues()
		{
			LabelFilter[] filters = [new LabelFilter { Find = "A", Replace = "B" }];
			LabelConfiguration config = new()
			{
				Dpmm = 8,
				LabelWidth = 4.0,
				LabelHeight = 6.0,
				Unit = LengthUnit.Inch,
				LabelRotation = 90,
				LabelFilters = filters
			};

			Assert.Equal(8, config.Dpmm);
			Assert.Equal(4.0, config.LabelWidth);
			Assert.Equal(6.0, config.LabelHeight);
			Assert.Equal(LengthUnit.Inch, config.Unit);
			Assert.Equal(90, config.LabelRotation);
			Assert.Equal(filters, config.LabelFilters);
		}
	}
}
