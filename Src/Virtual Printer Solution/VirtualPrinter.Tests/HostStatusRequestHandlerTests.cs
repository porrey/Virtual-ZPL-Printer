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
using ImageCache.Abstractions;
using Labelary.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Prism.Events;
using UnitsNet.Units;
using VirtualPrinter.Db.Abstractions;
using VirtualPrinter.Handler.HostStatus;
using Xunit;

namespace VirtualPrinter.Tests
{
	public class HostStatusRequestHandlerTests
	{
		private static HostStatusRequestHandler CreateHandler()
		{
			NullLogger<HostStatusRequestHandler> logger = new();
			Mock<IEventAggregator> eventAggregator = new();
			Mock<ILabelService> labelService = new();
			Mock<IImageCacheRepository> imageCacheRepository = new();
			return new HostStatusRequestHandler(logger, eventAggregator.Object, labelService.Object, imageCacheRepository.Object);
		}

		[Theory]
		[InlineData("~HS")]
		[InlineData("~hs")]
		[InlineData("~Hs")]
		[InlineData("~HS some data")]
		public async Task CanHandleRequestAsync_WithHsCommand_ReturnsTrue(string requestData)
		{
			HostStatusRequestHandler handler = CreateHandler();
			bool result = await handler.CanHandleRequestAsync(requestData);
			Assert.True(result);
		}

		[Theory]
		[InlineData("^XA^XZ")]
		[InlineData("NOP")]
		[InlineData("")]
		[InlineData("^FX test")]
		public async Task CanHandleRequestAsync_WithNonHsCommand_ReturnsFalse(string requestData)
		{
			HostStatusRequestHandler handler = CreateHandler();
			bool result = await handler.CanHandleRequestAsync(requestData);
			Assert.False(result);
		}

		[Fact]
		public async Task HandleRequest_ReturnsThreeStatusStrings()
		{
			HostStatusRequestHandler handler = CreateHandler();
			Mock<IPrinterConfiguration> printerConfig = new();
			LabelConfiguration labelConfig = new()
			{
				Dpmm = 8,
				LabelHeight = 6.0,
				LabelWidth = 4.0,
				Unit = LengthUnit.Inch,
				LabelFilters = []
			};

			(bool closeConnection, string responseData) = await handler.HandleRequest(printerConfig.Object, labelConfig, "~HS");

			Assert.True(closeConnection);
			Assert.NotNull(responseData);
			Assert.Contains("<STX>", responseData);
			Assert.Contains("<ETX>", responseData);

			string[] lines = responseData.Split(["\r\n"], StringSplitOptions.RemoveEmptyEntries);
			Assert.Equal(3, lines.Length);
		}

		[Fact]
		public async Task HandleRequest_StatusStringsHaveCorrectFormat()
		{
			HostStatusRequestHandler handler = CreateHandler();
			Mock<IPrinterConfiguration> printerConfig = new();
			LabelConfiguration labelConfig = new()
			{
				Dpmm = 8,
				LabelHeight = 1.0,
				LabelWidth = 4.0,
				Unit = LengthUnit.Inch,
				LabelFilters = []
			};

			(_, string responseData) = await handler.HandleRequest(printerConfig.Object, labelConfig, "~HS");

			Assert.StartsWith("<STX>", responseData.Trim().Split(["\r\n"], StringSplitOptions.None)[0]);
		}
	}
}
