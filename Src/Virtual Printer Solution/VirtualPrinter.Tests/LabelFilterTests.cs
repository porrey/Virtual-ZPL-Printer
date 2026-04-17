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
using Xunit;

namespace VirtualPrinter.Tests
{
	public class LabelFilterTests
	{
		[Fact]
		public void Properties_SetAndGet_CorrectValues()
		{
			LabelFilter filter = new()
			{
				Priority = 5,
				Find = "Hello",
				Replace = "World",
				TreatAsRegularExpression = true
			};

			Assert.Equal(5, filter.Priority);
			Assert.Equal("Hello", filter.Find);
			Assert.Equal("World", filter.Replace);
			Assert.True(filter.TreatAsRegularExpression);
		}
	}
}
