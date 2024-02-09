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
using System.Threading.Tasks;
using ImageCache.Abstractions;
using Labelary.Abstractions;
using Microsoft.Extensions.Logging;
using Prism.Events;
using VirtualPrinter.Db.Abstractions;

namespace VirtualPrinter
{
	internal class NopRequestHandler : TemplateRequestHandler
	{
		public NopRequestHandler(ILogger<NopRequestHandler> logger, IEventAggregator eventAggregator, ILabelService labelService, IImageCacheRepository imageCacheRepository)
			: base(logger, eventAggregator, labelService, imageCacheRepository)
		{
		}

		protected override Task<bool> OnCanHandleRequestAsync(string requestData)
		{
			bool returnValue = false;

			if (requestData.StartsWith("NOP"))
			{
				this.Logger.LogDebug("The NOP request handler has accepted the request '{request}'.", requestData);
				returnValue = true;
			}

			return Task.FromResult(returnValue);
		}

		protected override Task<(bool, string)> OnHandleRequest(IPrinterConfiguration printerConfiguration, ILabelConfiguration labelConfiguration, string requestData)
		{
			this.Logger.LogDebug("The NOP request handler is returning 'NOP'.");
			return Task.FromResult((true, "NOP"));
		}
	}
}
