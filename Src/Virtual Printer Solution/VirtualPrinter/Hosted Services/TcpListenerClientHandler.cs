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
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ImageCache.Abstractions;
using Labelary.Abstractions;
using Prism.Events;
using VirtualZplPrinter.Events;

namespace VirtualZplPrinter.Client
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
			client.ReceiveTimeout = 1000;
			client.SendTimeout = 1000;
			client.LingerState = new LingerOption(false, 0);
			client.NoDelay = true;
			client.ReceiveBufferSize = 8192;
			client.SendBufferSize = 8192;

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

							if (zpl != "NOP")
							{
								IGetLabelResponse response = await this.LabelService.GetLabelAsync(labelConfiguration, zpl);

								//
								// Save the image.
								//
								IStoredImage storedImage = await this.ImageCacheRepository.StoreImageAsync(imagePathRoot, response.Label);

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
									Label = storedImage,
									Result = response.Result,
									Message = response.Result ? "Label successfully created." : response.Error
								});
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
