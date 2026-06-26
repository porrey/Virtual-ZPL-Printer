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
using VirtualPrinter.Db.Abstractions;
using VirtualPrinter.GrfStorageService;
using VirtualPrinter.Handler.Abstractions;
using VirtualPrinter.PublishSubscribe;
using VirtualPrinter.ZplFormatService;

namespace VirtualPrinter.Handler.Zpl
{
	internal class ZplRequestHandler(ILogger<ZplRequestHandler> logger, IEventAggregator eventAggregator, ILabelService labelService, IImageCacheRepository imageCacheRepository, IGrfStorageService grfStorageService, IZplFormatService zplFormatService) : TemplateRequestHandler(logger, eventAggregator, labelService, imageCacheRepository)
	{
		protected IGrfStorageService GrfStorageService { get; } = grfStorageService;
		protected IZplFormatService ZplFormatService { get; } = zplFormatService;

		// A storage-only job has no printable output:
		//   ^DF  — template download; the real printer stores and produces nothing
		//   ~DG without ^FD — image download; no field data means no visible label
		private static bool IsStorageOnlyJob(string original, string expanded)
		{
			if (original.Contains("^DF"))  return true;
			if (original.Contains("~DG") && !expanded.Contains("^FD")) return true;
			return false;
		}

		protected override Task<bool> OnCanHandleRequestAsync(string requestData)
		{
			bool returnValue = true;

			this.Logger.LogDebug("The ZPL request handler has accepted the request '{request}' [showing first 25 characters].", requestData.Limit(25));

			return Task.FromResult(returnValue);
		}

		protected override async Task<(bool, string)> OnHandleRequest(IPrinterConfiguration printerConfiguration, ILabelConfiguration labelConfiguration, string requestData)
		{
			(bool closeConnection, string responseData) = (true, null);

			//
			// Capture any ~DG image blobs into the flash library.
			//
			await this.GrfStorageService.SaveGrfFromZplAsync(requestData);

			//
			// Capture any ^DF format templates into the flash library.
			//
			await this.ZplFormatService.SaveFormatFromZplAsync(requestData);

			//
			// Expand ^XF recall commands: load the stored template, substitute
			// ^FN placeholders with the ^FD field values from this print job.
			// This must run before GRF injection so that ^XG references inside
			// the recalled template body are also resolved.
			//
			string zplExpanded = await this.ZplFormatService.ApplyRecalledFormatsAsync(requestData);

			//
			// Inject ~DG blobs for any ^XG references (including those inside
			// a just-expanded template) so Labelary can resolve them inline.
			//
			string zplForLabelary = await this.GrfStorageService.ApplyReferencedGrfAsync(zplExpanded);

			//
			// Skip Labelary for pure flash-storage jobs — a ^DF template download
			// or a ~DG-only image load has no printable content. Sending these to
			// Labelary wastes API quota and the real Zebra printer produces no
			// output for them either.
			//
			if (IsStorageOnlyJob(requestData, zplForLabelary))
			{
				this.Logger.LogInformation("ZPL job is a flash storage operation (^DF or ~DG only) — skipping Labelary render.");
				return (closeConnection, responseData);
			}

			//
			// Get the label images from Labelary.
			//
			IEnumerable<IGetLabelResponse> responses = await this.LabelService.GetLabelsAsync(labelConfiguration, zplForLabelary);
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
			}

			return (closeConnection, responseData);
		}
	}
}
