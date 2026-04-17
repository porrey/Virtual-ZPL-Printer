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
	public class WarningTests
	{
		[Fact]
		public void ToString_WithZplCommand_ReturnsFormattedString()
		{
			Warning warning = new()
			{
				ByteIndex = 0,
				ByteSize = 5,
				ZplCommand = "^FX",
				ParameterNumber = 1,
				Message = "Invalid parameter"
			};
			Assert.Equal("^FX [parameter 1]: Invalid parameter", warning.ToString());
		}

		[Fact]
		public void ToString_WithNullZplCommand_UsesNA()
		{
			Warning warning = new()
			{
				ZplCommand = null,
				ParameterNumber = 2,
				Message = "Some warning"
			};
			Assert.Equal("N/A [parameter 2]: Some warning", warning.ToString());
		}

		[Fact]
		public void ToString_WithEmptyZplCommand_UsesNA()
		{
			Warning warning = new()
			{
				ZplCommand = "",
				ParameterNumber = 0,
				Message = "Some warning"
			};
			Assert.Equal("N/A [parameter 0]: Some warning", warning.ToString());
		}
	}
}
