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
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ImageCache.Abstractions;
using Labelary.Abstractions;
using Prism.Events;
using VirtualPrinter.Events;

namespace VirtualPrinter.Client
{
	public class TcpListenerClientHandler
	{
		public TcpListenerClientHandler(IEventAggregator eventAggregator, ILabelService labelService, IImageCacheRepository imageCacheRepository)
		{
			this.EventAggregator = eventAggregator;
			this.LabelService = labelService;
			this.ImageCacheRepository = imageCacheRepository;
		}

		protected IEventAggregator EventAggregator { get; set; }
		protected ILabelService LabelService { get; set; }
		protected IImageCacheRepository ImageCacheRepository { get; set; }

		public async Task StartSessionAsync(TcpClient client, ILabelConfiguration labelConfiguration, string imagePathRoot)
		{
			//
			// Set parameters.
			//
			client.ReceiveTimeout = Properties.Settings.Default.ReceiveTimeout;
			client.SendTimeout = Properties.Settings.Default.SendTimeout;
			client.NoDelay = Properties.Settings.Default.NoDelay;
			client.ReceiveBufferSize = Properties.Settings.Default.ReceiveBufferSize;
			client.SendBufferSize = Properties.Settings.Default.SendBufferSize;
			client.LingerState = new LingerOption(Properties.Settings.Default.Linger, Properties.Settings.Default.LingerTime);

			//
			// Get the network stream.
			//
			NetworkStream stream = client.GetStream();

			while (client.Connected && stream.CanRead)
			{
				if (stream.DataAvailable)
				{
					try
					{
						//
						// Create a buffer for the data that is available.
						//
						byte[] buffer = new byte[client.Available];

						//
						// Read the data into the buffer.
						//
						int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));

						if (bytesRead > 0)
						{
							//
							// Get the label image
							//
							string zpl = Encoding.UTF8.GetString(buffer);

							if (!zpl.StartsWith("NOP"))
							{
								//
								// Get the label images from Labelary.
								//
								IEnumerable<IGetLabelResponse> responses = await this.LabelService.GetLabelsAsync(labelConfiguration, zpl);

								//
								// Save the images.
								//
								IEnumerable<IStoredImage> storedImages = await this.ImageCacheRepository.StoreLabelImagesAsync(imagePathRoot, responses);

								//
								// Publish the images.
								//
								foreach (IGetLabelResponse response in responses)
								{
									//
									// Publish the new label.
									//
									this.EventAggregator.GetEvent<LabelCreatedEvent>().Publish(new LabelCreatedEventArgs()
									{
										PrintRequest = new PrintRequestEventArgs()
										{
											LabelConfiguration = labelConfiguration,
											Zpl = zpl
										},
										Label = storedImages.ElementAt(response.LabelIndex),
										Result = response.Result,
										Message = response.Result ? "Label successfully created." : response.Error
									});
								}
							}
						}
					}
					finally
					{
						client.Close();
					}
				}
			}
		}
	}
}
