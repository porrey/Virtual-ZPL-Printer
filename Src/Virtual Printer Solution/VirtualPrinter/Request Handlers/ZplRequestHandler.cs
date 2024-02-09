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
using ImageCache.Abstractions;
using Labelary.Abstractions;
using Microsoft.Extensions.Logging;
using Prism.Events;
using VirtualPrinter.Db.Abstractions;
using VirtualPrinter.Events;

namespace VirtualPrinter
{
	internal class ZplRequestHandler : TemplateRequestHandler
	{
		public ZplRequestHandler(ILogger<ZplRequestHandler> logger, IEventAggregator eventAggregator, ILabelService labelService, IImageCacheRepository imageCacheRepository)
			: base(logger, eventAggregator, labelService, imageCacheRepository)
		{
		}

		protected override Task<bool> OnCanHandleRequestAsync(string requestData)
		{
			bool returnValue = false;

			this.Logger.LogDebug("The ZPL request handler has accepted the request '{request}' [showing first 25 characters].", requestData.Limit(25));
			returnValue = true;

			return Task.FromResult(returnValue);
		}

		protected override async Task<(bool, string)> OnHandleRequest(IPrinterConfiguration printerConfiguration, ILabelConfiguration labelConfiguration, string requestData)
		{
			(bool closeConnection, string responseData) = (false, null);

			//
			// Get the label images from Labelary.
			//
			IEnumerable<IGetLabelResponse> responses = await this.LabelService.GetLabelsAsync(labelConfiguration, requestData);
			this.Logger.LogInformation("The ZPL handler retrieved {count} response(s) from Labelary.", responses.Count());

			if (responses.Any())
			{
				//
				// Save the images.
				//
				IEnumerable<IStoredImage> storedImages = await this.ImageCacheRepository.StoreLabelImagesAsync(printerConfiguration.ImagePath, responses);
				this.Logger.LogInformation("The ZPL handler saved {count} image(s).", storedImages.Count());

				//
				// Publish the images.
				//
				foreach (IGetLabelResponse labelResponse in responses)
				{
					this.Logger.LogDebug("Raising event for label response {name}.", labelResponse.ImageFileName);

					//
					// Publish the new label.
					//
					this.EventAggregator.GetEvent<LabelCreatedEvent>().Publish(new LabelCreatedEventArgs()
					{
						PrinterConfiguration = printerConfiguration,
						PrintRequest = new PrintRequestEventArgs()
						{
							LabelConfiguration = labelConfiguration,
							Zpl = requestData
						},
						Label = storedImages.ElementAt(labelResponse.LabelIndex),
						Result = labelResponse.Result,
						Message = labelResponse.Result ? "Label successfully created." : labelResponse.Error,
						Warnings = labelResponse.Warnings
					});
				}

				closeConnection = true;
			}
			else
			{
				closeConnection = true;
			}

			return (closeConnection, responseData);
		}
	}
}
