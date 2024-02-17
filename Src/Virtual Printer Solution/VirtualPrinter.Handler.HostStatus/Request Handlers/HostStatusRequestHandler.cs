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
using Microsoft.Extensions.Logging;
using Prism.Events;
using UnitsNet;
using UnitsNet.Units;
using VirtualPrinter.Db.Abstractions;
using VirtualPrinter.Handler.Abstractions;

namespace VirtualPrinter.Handler.HostStatus
{
	internal class HostStatusRequestHandler(ILogger<HostStatusRequestHandler> logger, IEventAggregator eventAggregator, ILabelService labelService, IImageCacheRepository imageCacheRepository) : TemplateRequestHandler(logger, eventAggregator, labelService, imageCacheRepository)
	{
		protected override Task<bool> OnCanHandleRequestAsync(string requestData)
		{
			bool returnValue = false;

			if (requestData != null && requestData.StartsWith("~HS", StringComparison.CurrentCultureIgnoreCase))
			{
				this.Logger.LogDebug("The ~HS request handler has accepted the request '{request}'.", requestData);
				returnValue = true;
			}

			return Task.FromResult(returnValue);
		}

		protected override Task<(bool, string)> OnHandleRequest(IPrinterConfiguration printerConfiguration, ILabelConfiguration labelConfiguration, string requestData)
		{
			(bool closeConnection, string responseData) = (true, null);

			//
			// Status request.
			// The return format is:
			// String 1: <STX>aaa,b,c,dddd,eee,f,g,h,iii,j,k,l<ETX><CR><LF>
			// String 2: <STX>mmm,n,o,p,q,r,s,t,uuuuuuuu,v,www<ETX><CR><LF>
			// String 3: <STX>xxxx,y<ETX><CR><LF>
			// https://support.zebra.com/cpws/docs/zpl/HS_Command.pdf
			//
			this.Logger.LogDebug("The ~HS handler is returning the printer status.");
			int height = (int)(labelConfiguration.Dpmm * new Length(labelConfiguration.LabelHeight, labelConfiguration.Unit).ToUnit(LengthUnit.Millimeter).Value);

			string string1 = $"<STX>000,0,0,{height},0,0,0,0,000,0,0,0<ETX>\r\n";
			string string2 = $"<STX>000,0,0,0,1,2,0,0,00000000,1,000<ETX>\r\n";
			string string3 = $"<STX>none,0<ETX>\r\n";

			responseData = $"{string1}{string2}{string3}";

			return Task.FromResult((closeConnection, responseData));
		}
	}
}
