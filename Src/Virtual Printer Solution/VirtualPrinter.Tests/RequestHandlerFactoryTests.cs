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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Prism.Events;
using VirtualPrinter.Db.Abstractions;
using VirtualPrinter.Handler.Abstractions;
using VirtualPrinter.Handler.HostStatus;
using VirtualPrinter.Handler.Nop;
using Xunit;

namespace VirtualPrinter.Tests
{
	public class RequestHandlerFactoryTests
	{
		private static RequestHandlerFactory CreateFactory(params IRequestHandler[] handlers)
		{
			NullLogger<RequestHandlerFactory> logger = new();
			ServiceCollection services = new();
			foreach (IRequestHandler h in handlers)
			{
				services.AddSingleton(h);
			}
			IServiceProvider sp = services.BuildServiceProvider();
			return new RequestHandlerFactory(logger, sp);
		}

		private static NopRequestHandler CreateNopHandler()
		{
			NullLogger<NopRequestHandler> logger = new();
			Mock<IEventAggregator> eventAggregator = new();
			Mock<ILabelService> labelService = new();
			Mock<IImageCacheRepository> imageCacheRepository = new();
			NopRequestHandler handler = new(logger, eventAggregator.Object, labelService.Object, imageCacheRepository.Object);
			handler.Priority = 1;
			return handler;
		}

		private static HostStatusRequestHandler CreateHostStatusHandler()
		{
			NullLogger<HostStatusRequestHandler> logger = new();
			Mock<IEventAggregator> eventAggregator = new();
			Mock<ILabelService> labelService = new();
			Mock<IImageCacheRepository> imageCacheRepository = new();
			HostStatusRequestHandler handler = new(logger, eventAggregator.Object, labelService.Object, imageCacheRepository.Object);
			handler.Priority = 2;
			return handler;
		}

		[Fact]
		public async Task GetHandlerAsync_WithMatchingHandler_ReturnsCorrectHandler()
		{
			NopRequestHandler nopHandler = CreateNopHandler();
			RequestHandlerFactory factory = CreateFactory(nopHandler);

			IRequestHandler result = await factory.GetHandlerAsync("NOP");

			Assert.NotNull(result);
			Assert.IsType<NopRequestHandler>(result);
		}

		[Fact]
		public async Task GetHandlerAsync_WithNoMatchingHandler_ReturnsNull()
		{
			NopRequestHandler nopHandler = CreateNopHandler();
			RequestHandlerFactory factory = CreateFactory(nopHandler);

			IRequestHandler result = await factory.GetHandlerAsync("^XA^XZ");

			Assert.Null(result);
		}

		[Fact]
		public async Task GetHandlerAsync_SelectsHandlerByPriority()
		{
			NopRequestHandler nopHandler = CreateNopHandler();         // Priority 1
			HostStatusRequestHandler hostStatusHandler = CreateHostStatusHandler(); // Priority 2
			RequestHandlerFactory factory = CreateFactory(hostStatusHandler, nopHandler);

			IRequestHandler result = await factory.GetHandlerAsync("~HS");

			Assert.IsType<HostStatusRequestHandler>(result);
		}
	}
}
