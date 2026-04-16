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
	public class GetLabelResponseTests
	{
		[Fact]
		public void HasMultipleLabels_WhenLabelCountGreaterThanOne_ReturnsTrue()
		{
			GetLabelResponse response = new() { LabelCount = 3 };
			Assert.True(response.HasMultipleLabels);
		}

		[Fact]
		public void HasMultipleLabels_WhenLabelCountIsOne_ReturnsFalse()
		{
			GetLabelResponse response = new() { LabelCount = 1 };
			Assert.False(response.HasMultipleLabels);
		}

		[Fact]
		public void HasMultipleLabels_WhenLabelCountIsZero_ReturnsFalse()
		{
			GetLabelResponse response = new() { LabelCount = 0 };
			Assert.False(response.HasMultipleLabels);
		}

		[Fact]
		public void Properties_SetAndGet_CorrectValues()
		{
			byte[] labelData = [1, 2, 3];
			Warning[] warnings = [new Warning { Message = "test" }];
			GetLabelResponse response = new()
			{
				LabelIndex = 2,
				LabelCount = 5,
				Result = true,
				Label = labelData,
				Error = null,
				ImageFileName = "test-label",
				Warnings = warnings,
				Zpl = "^XA^XZ"
			};

			Assert.Equal(2, response.LabelIndex);
			Assert.Equal(5, response.LabelCount);
			Assert.True(response.Result);
			Assert.Equal(labelData, response.Label);
			Assert.Null(response.Error);
			Assert.Equal("test-label", response.ImageFileName);
			Assert.Equal(warnings, response.Warnings);
			Assert.Equal("^XA^XZ", response.Zpl);
		}
	}
}
