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
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
			// ReceiveTimeout and SendTimeout only apply when using *synchronous* read/write
			// https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.receivetimeout

			client.NoDelay = Properties.Settings.Default.NoDelay;
			client.ReceiveBufferSize = Properties.Settings.Default.ReceiveBufferSize;
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
						string zpl = await ReadZpl(stream);
						if (zpl.Trim().Length > 0)
						{
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

		public async Task<string> ReadZpl(NetworkStream stream)
		{
			Debug.WriteLine("Reading ZPL...");
			var cts = new CancellationTokenSource(Properties.Settings.Default.ReceiveTimeout);
			var buffer = new byte[Properties.Settings.Default.ReceiveBufferSize];
			var resultString = "";

			// Read all bytes until ReadAsync returns 0 (connection closed) or a timeout occured
			// Do not check stream.DataAvailable, as this might result in incomplete reads if the server is reading faster than the client is sending
			try
			{
				while (true)
				{
					int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);

					if (bytesRead == 0)
					{
						// Return immediately when connection was closed
						return resultString;
					}

					resultString += Encoding.UTF8.GetString(buffer, 0, bytesRead);

					if (resultString.TrimEnd().EndsWith("^XZ"))
					{
						// Return early if end of label is detected (even if the connection was not closed)
						cts.CancelAfter(100);
					}
				}
			}
			catch (OperationCanceledException)
			{
				// Ignore OperationCanceledException and return data received so far
			}

			return resultString;
		}
	}
}
