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
using Labelary.Service;
using Xunit;

namespace VirtualPrinter.Tests
{
	public class ZplDecoratorTests
	{
		[Fact]
		public void Filter_WithNoFilters_ReturnsOriginalZpl()
		{
			string zpl = "^XA^FO50,50^A0N,50,50^FDHello^FS^XZ";
			string result = zpl.Filter([]);
			Assert.Equal(zpl, result);
		}

		[Fact]
		public void Filter_WithPlainTextFilter_ReplacesText()
		{
			string zpl = "^XA^FDHello World^FS^XZ";
			ILabelFilter[] filters =
			[
				new LabelFilter { Priority = 1, Find = "Hello", Replace = "Hi", TreatAsRegularExpression = false }
			];
			string result = zpl.Filter(filters);
			Assert.Equal("^XA^FDHi World^FS^XZ", result);
		}

		[Fact]
		public void Filter_WithRegexFilter_ReplacesMatch()
		{
			string zpl = "^XA^FD12345^FS^XZ";
			ILabelFilter[] filters =
			[
				new LabelFilter { Priority = 1, Find = @"\d+", Replace = "NUM", TreatAsRegularExpression = true }
			];
			string result = zpl.Filter(filters);
			Assert.Equal("^XA^FDNUM^FS^XZ", result);
		}

		[Fact]
		public void Filter_WithInvalidRegex_ReturnsOriginalZpl()
		{
			string zpl = "^XA^FDHello^FS^XZ";
			ILabelFilter[] filters =
			[
				new LabelFilter { Priority = 1, Find = "[invalid", Replace = "x", TreatAsRegularExpression = true }
			];
			string result = zpl.Filter(filters);
			Assert.Equal(zpl, result);
		}

		[Fact]
		public void Filter_AppliesFiltersInPriorityOrder()
		{
			string zpl = "ABC";
			ILabelFilter[] filters =
			[
				new LabelFilter { Priority = 2, Find = "B", Replace = "Y", TreatAsRegularExpression = false },
				new LabelFilter { Priority = 1, Find = "A", Replace = "X", TreatAsRegularExpression = false }
			];
			string result = zpl.Filter(filters);
			Assert.Equal("XYC", result);
		}

		[Fact]
		public void Filter_WithNullReplace_RemovesText()
		{
			string zpl = "^XA^FDHello^FS^XZ";
			ILabelFilter[] filters =
			[
				new LabelFilter { Priority = 1, Find = "Hello", Replace = null, TreatAsRegularExpression = false }
			];
			string result = zpl.Filter(filters);
			Assert.Equal("^XA^FD^FS^XZ", result);
		}

		[Fact]
		public void GetParameterValue_WithMatchingParameter_ReturnsValue()
		{
			string zpl = "^XA\r\n^FX ImageFileName: my-label\r\n^FDHello^FS^XZ";
			string result = zpl.GetParameterValue("ImageFileName", "default");
			Assert.Equal(" my-label", result);
		}

		[Fact]
		public void GetParameterValue_WithNoMatchingParameter_ReturnsDefault()
		{
			string zpl = "^XA^FDHello^FS^XZ";
			string result = zpl.GetParameterValue("ImageFileName", "default-name");
			Assert.Equal("default-name", result);
		}

		[Fact]
		public void GetParameterValue_IsCaseInsensitiveForParameterName()
		{
			string zpl = "^XA\r\n^FX IMAGEFILENAME: test-label\r\n^XZ";
			string result = zpl.GetParameterValue("imagefilename", "default");
			Assert.Equal(" test-label", result);
		}
	}
}
