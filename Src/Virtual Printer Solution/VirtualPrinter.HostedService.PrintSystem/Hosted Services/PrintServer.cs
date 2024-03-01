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
using System.Drawing;
using System.Drawing.Printing;
using Diamond.Core.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Prism.Events;
using VirtualPrinter.PublishSubscribe;

namespace VirtualPrinter.HostedService.PrintSystem
{
	internal class PrintServer : HostedServiceTemplate
	{
		public PrintServer(ILogger<PrintServer> logger, IHostApplicationLifetime hostApplicationLifetime, IEventAggregator eventAggregator, IServiceScopeFactory serviceScopeFactory)
			: base(hostApplicationLifetime, logger, serviceScopeFactory)
		{
			this.EventAggregator = eventAggregator;
			this.ServiceScopeFactory = serviceScopeFactory;
		}

		protected IEventAggregator EventAggregator { get; set; }
		protected SubscriptionToken SubscriptionToken { get; set; }

		protected override void OnStarted()
		{
			try
			{
				this.Logger.LogInformation("Starting the printer service.");
				this.SubscriptionToken = this.EventAggregator.GetEvent<LabelCreatedEvent>().Subscribe((e) => this.OnPhysicalPrintRequestEvent(e), ThreadOption.BackgroundThread);
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, "Exception in {class}.{name}().", nameof(PrintServer), nameof(OnStarted));
			}
		}

		protected void OnPhysicalPrintRequestEvent(LabelCreatedEventArgs e)
		{
			try
			{
				if (e.PrinterConfiguration.PhysicalPrinter != null)
				{
					PhysicalPrinter physicalPrinter = JsonConvert.DeserializeObject<PhysicalPrinter>(e.PrinterConfiguration.PhysicalPrinter);

					if (physicalPrinter.Enabled && this.PrinterExists(physicalPrinter.PrinterName) && e.Result)
					{
						this.Logger.LogDebug("Sending label to printer '{name}'.", physicalPrinter.PrinterName);

						//
						// Start the print on another thread.
						//
						Task.Factory.StartNew(() =>
						{
							this.PrintLabel(physicalPrinter, e);
						});
					}
				}
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, "Exception in {class}.{name}().", nameof(PrintServer), nameof(OnPhysicalPrintRequestEvent));
			}
		}

		protected override Task OnBeginStopAsync()
		{
			try
			{
				this.Logger.LogDebug("Shutting down the printer service.");
				this.SubscriptionToken?.Dispose();
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, "Exception in {class}.{name}().", nameof(PrintServer), nameof(OnBeginStopAsync));
			}

			return Task.CompletedTask;
		}

		private bool PrinterExists(string printerName)
		{
			bool returnValue = false;

			foreach (object printer in PrinterSettings.InstalledPrinters)
			{
				if (Convert.ToString(printer) == printerName)
				{
					returnValue = true;
					break;
				}
			}

			return returnValue;
		}

		private bool PrintLabel(IPhysicalPrinter physicalPrinter, LabelCreatedEventArgs labelCreatedEventArgs)
		{
			bool returnValue = false;

			try
			{
				//
				// Create a PrintDocument object.
				//
				using (PrintDocument doc = new())
				{
					//
					// Attach a handler to the PrintPage event.
					//
					doc.PrintPage += (s, e) =>
					{
						//
						// Determine the scaling.
						//
						int scaleX = e.Graphics.PageUnit == GraphicsUnit.Display ? 100 : e.PageSettings.PrinterResolution.X;
						int scaleY = e.Graphics.PageUnit == GraphicsUnit.Display ? 100 : e.PageSettings.PrinterResolution.Y;

						//
						// Get the image DPI.
						//
						float imageDpi = Convert.ToSingle(labelCreatedEventArgs.PrintRequest.LabelConfiguration.Dpmm * 25.4);

						using (Bitmap image = new(Image.FromFile(labelCreatedEventArgs.Label.FullPath)))
						{
							//
							// Set the image resolution based on the original ZPL label.
							//
							image.SetResolution(imageDpi, imageDpi);

							//
							// Get the image target width
							//
							float width = image.Width / image.HorizontalResolution;
							float height = image.Height / image.VerticalResolution;

							RectangleF rect = new()
							{
								X = this.X(physicalPrinter, e.PageBounds.Width, width, scaleX),
								Y = this.Y(physicalPrinter, e.PageBounds.Height, height, scaleY),
								Width = scaleX * width,
								Height = scaleY * height
							};

							e.Graphics.DrawImage(image, rect);
							e.HasMorePages = false;
						}
					};

					//
					// Set the document properties.
					//
					doc.PrinterSettings.PrinterName = physicalPrinter.PrinterName;
					doc.PrinterSettings.Collate = false;
					doc.PrinterSettings.Copies = 1;

					//
					// Print the document.
					//
					doc.Print();
				}
			}
			catch (Exception ex)
			{
				this.Logger.LogError("Exception: '{ex}'.", ex.Message);
			}

			return returnValue;
		}

		private float X(IPhysicalPrinter physicalPrinter, int pageWidth, float imageWidth, float scaleX)
		{
			float returnValue = 0;

			if (physicalPrinter.HorizontalAlignLeft)
			{
				returnValue = Convert.ToSingle(scaleX * physicalPrinter.LeftMargin);
			}
			else if (physicalPrinter.HorizontalAlignCenter)
			{
				returnValue = (pageWidth - imageWidth) / 2F;
			}
			else if (physicalPrinter.HorizontalAlignRight)
			{
				returnValue = Convert.ToSingle(pageWidth - physicalPrinter.RightMargin - imageWidth);
			}

			return returnValue;
		}

		private float Y(IPhysicalPrinter physicalPrinter, int pageHeight, float imageHeight, float scaleY)
		{
			float returnValue = 0;

			if (physicalPrinter.VerticalAlignTop)
			{
				returnValue = Convert.ToSingle(scaleY * physicalPrinter.TopMargin);
			}
			else if (physicalPrinter.VerticalAlignMiddle)
			{
				returnValue = (pageHeight - imageHeight) / 2F;
			}
			else if (physicalPrinter.VerticalAlignBottom)
			{
				returnValue = Convert.ToSingle(pageHeight - physicalPrinter.BottomMargin - imageHeight);
			}

			return returnValue;
		}
	}
}
