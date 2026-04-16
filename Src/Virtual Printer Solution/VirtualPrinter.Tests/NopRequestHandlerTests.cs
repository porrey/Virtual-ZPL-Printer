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
using VirtualPrinter.Handler.Nop;
using Xunit;

namespace VirtualPrinter.Tests
{
	public class NopRequestHandlerTests
	{
		private static NopRequestHandler CreateHandler()
		{
			NullLogger<NopRequestHandler> logger = new();
			Mock<IEventAggregator> eventAggregator = new();
			Mock<ILabelService> labelService = new();
			Mock<IImageCacheRepository> imageCacheRepository = new();
			return new NopRequestHandler(logger, eventAggregator.Object, labelService.Object, imageCacheRepository.Object);
		}

		[Fact]
		public async Task CanHandleRequestAsync_WithNopPrefix_ReturnsTrue()
		{
			NopRequestHandler handler = CreateHandler();
			bool result = await handler.CanHandleRequestAsync("NOP");
			Assert.True(result);
		}

		[Fact]
		public async Task CanHandleRequestAsync_WithNopAndData_ReturnsTrue()
		{
			NopRequestHandler handler = CreateHandler();
			bool result = await handler.CanHandleRequestAsync("NOP some data");
			Assert.True(result);
		}

		[Theory]
		[InlineData("^XA^XZ")]
		[InlineData("~HS")]
		[InlineData("")]
		[InlineData("nop")]
		public async Task CanHandleRequestAsync_WithNonNopCommand_ReturnsFalse(string requestData)
		{
			NopRequestHandler handler = CreateHandler();
			bool result = await handler.CanHandleRequestAsync(requestData);
			Assert.False(result);
		}

		[Fact]
		public async Task HandleRequest_ReturnsNopResponse()
		{
			NopRequestHandler handler = CreateHandler();
			Mock<IPrinterConfiguration> printerConfig = new();
			LabelConfiguration labelConfig = new()
			{
				Dpmm = 8,
				LabelHeight = 6.0,
				LabelWidth = 4.0,
				Unit = LengthUnit.Inch,
				LabelFilters = []
			};

			(bool closeConnection, string responseData) = await handler.HandleRequest(printerConfig.Object, labelConfig, "NOP");

			Assert.True(closeConnection);
			Assert.Equal("NOP", responseData);
		}
	}
}
