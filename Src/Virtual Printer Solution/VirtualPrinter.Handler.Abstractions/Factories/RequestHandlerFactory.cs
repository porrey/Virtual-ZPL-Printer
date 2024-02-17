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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace VirtualPrinter.Handler.Abstractions
{
	internal class RequestHandlerFactory(ILogger<RequestHandlerFactory> logger, IServiceProvider serviceProvider) : IRequestHandlerFactory
	{
		protected ILogger<RequestHandlerFactory> Logger { get; set; } = logger;
		protected virtual IServiceProvider ServiceProvider { get; set; } = serviceProvider;

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
