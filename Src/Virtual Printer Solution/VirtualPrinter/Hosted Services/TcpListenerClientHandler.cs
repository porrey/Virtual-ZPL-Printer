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
using System.IO;
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

			//
			// Prepare a memory stream to read data into.
			//
			MemoryStream ms = new MemoryStream();

			try
			{
				if (client.Connected && stream.CanRead)
				{
					//
					// Set up a temporary buffer.
					//
					byte[] data = new byte[1024];

					//
					// Read from the NetworkStream until there isn't any data to read available anymore.
					//
					// See https://stackoverflow.com/questions/26058594/how-to-get-all-data-from-networkstream
					//
					// Unfortunately, that doesn't mean that all data has necessarily been received from the client,
					// so we could still haven't received some data after all, e.g. due to network delays. It would
					// be better to try to read all data until the connection is terminated by the client, however,
					// there isn't an easy way to detect a closed connection with a NetworkStream.
					//
					// By reading the data in small chunks, we're giving the client additional time to send
					// data - the larger the data to be received, the more time available. Finally, by increasing
					// the read timeout, we can further easily increase the time available to the client.
					//
					int numBytesRead;
					while ((numBytesRead = stream.Read(data, 0, data.Length)) > 0)
					{
					    ms.Write(data, 0, numBytesRead);
					}
				}
			}
			finally
			{
				client.Close();
			}

			//
			// Only try to create a label image if any data has been received in the first place.
			//
			if (ms.Length > 0)
			{
				//
				// Use user-specified encoding in order to display special characters correctly.
				//
				Encoding encoding = Encoding.UTF8;
				try
				{
					encoding = Encoding.GetEncoding(Properties.Settings.Default.ReceivedDataEncoding);
				}
				catch (ArgumentException ex)
				{
					//
					// Simply fallback to default encoding and ignore exception.
					//
				}

				//
				// Get the label image.
				//
				string zpl = encoding.GetString(ms.ToArray(), 0, (int) ms.Length);

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
	}
}
