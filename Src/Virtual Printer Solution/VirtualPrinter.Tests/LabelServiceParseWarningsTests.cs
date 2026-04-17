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
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VirtualPrinter.ApplicationSettings;
using VirtualPrinter.FontService;
using Xunit;

namespace VirtualPrinter.Tests
{
	/// <summary>
	/// Exposes the protected ParseWarnings method for testing.
	/// </summary>
	public class TestableLabelService : LabelService
	{
		public TestableLabelService()
			: base(
				  new NullLogger<LabelService>(),
				  new LabelServiceConfiguration(CreateSettings()),
				  CreateFontService())
		{
		}

		private static ISettings CreateSettings()
		{
			Mock<ISettings> settings = new();
			settings.Setup(s => s.ApiUrl).Returns("http://api.labelary.com/v1/printers");
			settings.Setup(s => s.ApiMethod).Returns("POST");
			settings.Setup(s => s.ApiLinting).Returns(false);
			return settings.Object;
		}

		private static IFontService CreateFontService()
		{
			Mock<IFontService> fontService = new();
			fontService.Setup(f => f.GetReferencedFontsAsync(It.IsAny<string>()))
				.ReturnsAsync([]);
			fontService.Setup(f => f.ApplyReferencedFontsAsync(It.IsAny<IEnumerable<IPrinterFont>>(), It.IsAny<string>()))
				.ReturnsAsync((IEnumerable<IPrinterFont> fonts, string zpl) => zpl);
			return fontService.Object;
		}

		public new IEnumerable<Warning> ParseWarnings(string warnings) => base.ParseWarnings(warnings);
	}

	public class LabelServiceParseWarningsTests
	{
		[Fact]
		public void ParseWarnings_WithNullInput_ReturnsEmpty()
		{
			TestableLabelService service = new();
			IEnumerable<Warning> result = service.ParseWarnings(null);
			Assert.Empty(result);
		}

		[Fact]
		public void ParseWarnings_WithEmptyInput_ReturnsEmpty()
		{
			TestableLabelService service = new();
			IEnumerable<Warning> result = service.ParseWarnings("");
			Assert.Empty(result);
		}

		[Fact]
		public void ParseWarnings_WithWhitespaceInput_ReturnsEmpty()
		{
			TestableLabelService service = new();
			IEnumerable<Warning> result = service.ParseWarnings("   ");
			Assert.Empty(result);
		}

		[Fact]
		public void ParseWarnings_WithSingleWarning_ReturnsOneWarning()
		{
			TestableLabelService service = new();
			string warnings = "0|5|^FX|1|Invalid parameter";
			IEnumerable<Warning> result = service.ParseWarnings(warnings);
			Warning warning = Assert.Single(result);
			Assert.Equal(0, warning.ByteIndex);
			Assert.Equal(5, warning.ByteSize);
			Assert.Equal("^FX", warning.ZplCommand);
			Assert.Equal(1, warning.ParameterNumber);
			Assert.Equal("Invalid parameter", warning.Message);
		}

		[Fact]
		public void ParseWarnings_WithMultipleWarnings_ReturnsAllWarnings()
		{
			TestableLabelService service = new();
			string warnings = "0|5|^FX|1|First warning|10|3|^FD|2|Second warning";
			IEnumerable<Warning> result = service.ParseWarnings(warnings);
			Assert.Equal(2, result.Count());
		}

		[Fact]
		public void ParseWarnings_WithEmptyParameterNumber_UsesZero()
		{
			TestableLabelService service = new();
			string warnings = "0|5|^FX||No parameter";
			IEnumerable<Warning> result = service.ParseWarnings(warnings);
			Warning warning = Assert.Single(result);
			Assert.Equal(0, warning.ParameterNumber);
		}
	}
}
