﻿/*
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
using Microsoft.Extensions.Logging;
using Prism.Events;
using VirtualPrinter.Db.Abstractions;

namespace VirtualPrinter.Handler.Abstractions
{
	public abstract class TemplateRequestHandler(ILogger<TemplateRequestHandler> logger, IEventAggregator eventAggregator, ILabelService labelService, IImageCacheRepository imageCacheRepository) : IRequestHandler
	{
		protected ILogger<TemplateRequestHandler> Logger { get; set; } = logger;
		protected IEventAggregator EventAggregator { get; set; } = eventAggregator;
		protected ILabelService LabelService { get; set; } = labelService;
		protected IImageCacheRepository ImageCacheRepository { get; set; } = imageCacheRepository;

		public int Priority { get; set; }

		public Task<bool> CanHandleRequestAsync(string requestData)
		{
			return this.OnCanHandleRequestAsync(requestData);
		}

		public Task<(bool, string)> HandleRequest(IPrinterConfiguration printerConfiguration, ILabelConfiguration labelConfiguration, string requestData)
		{
			return this.OnHandleRequest(printerConfiguration, labelConfiguration, requestData);
		}

		protected virtual Task<bool> OnCanHandleRequestAsync(string requestData)
		{
			return Task.FromResult(false);
		}

		protected virtual Task<(bool, string)> OnHandleRequest(IPrinterConfiguration printerConfiguration, ILabelConfiguration labelConfiguration, string requestData)
		{
			return Task.FromResult<(bool, string)>((true, null));
		}
	}
}
