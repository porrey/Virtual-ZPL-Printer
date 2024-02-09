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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VirtualPrinter.Abstractions;

namespace VirtualPrinter
{
	internal class RequestHandlerFactory : IRequestHandlerFactory
	{
		public RequestHandlerFactory(ILogger<RequestHandlerFactory> logger, IServiceProvider serviceProvider)
		{
			this.Logger = logger;
			this.ServiceProvider = serviceProvider;
		}

		protected ILogger<RequestHandlerFactory> Logger { get; set; }
		protected virtual IServiceProvider ServiceProvider { get; set; }

		public async Task<IRequestHandler> GetHandlerAsync(string requestData)
		{
			IRequestHandler returnValue = null;

			IEnumerable<IRequestHandler> items = this.ServiceProvider.GetService<IEnumerable<IRequestHandler>>();
			this.Logger.LogDebug("There are {count} handler(s) configured.", items.Count());

			foreach (IRequestHandler item in items.OrderBy(t => t.Priority))
			{
				if (await item.CanHandleRequestAsync(requestData))
				{
					this.Logger.LogDebug("The request handler '{name}' will handle this request.", item.GetType().Name);
					returnValue = item;
					break;
				}
				else
				{
					this.Logger.LogDebug("The request handler '{name}' does not handle this request.", item.GetType().Name);
				}
			}

			return returnValue;
		}
	}
}
