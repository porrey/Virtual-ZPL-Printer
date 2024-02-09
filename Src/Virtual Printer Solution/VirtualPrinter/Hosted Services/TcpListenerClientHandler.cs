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
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ImageCache.Abstractions;
using Labelary.Abstractions;
using Microsoft.Extensions.Logging;
using Prism.Events;
using VirtualPrinter.Abstractions;
using VirtualPrinter.Db.Abstractions;

namespace VirtualPrinter.Client
{
	public class TcpListenerClientHandler
	{
		public TcpListenerClientHandler(ILogger<TcpListenerClientHandler> logger, IEventAggregator eventAggregator, IRequestHandlerFactory requestHandlerFactory, ILabelService labelService, IImageCacheRepository imageCacheRepository)
		{
			this.Logger = logger;
			this.EventAggregator = eventAggregator;
			this.RequestHandlerFactory = requestHandlerFactory;
			this.LabelService = labelService;
			this.ImageCacheRepository = imageCacheRepository;
		}

		protected ILogger<TcpListenerClientHandler> Logger { get; set; }
		protected IEventAggregator EventAggregator { get; set; }
		protected IRequestHandlerFactory RequestHandlerFactory { get; set; }
		protected ILabelService LabelService { get; set; }
		protected IImageCacheRepository ImageCacheRepository { get; set; }

		public async Task StartSessionAsync(TcpClient client, IPrinterConfiguration printerConfiguration, ILabelConfiguration labelConfiguration, string imagePathRoot)
		{
			this.Logger.LogInformation("Handling incoming request from {endpoint}.", client.Client.LocalEndPoint);

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
			// Use user-specified encoding in order to display special characters correctly.
			//
			Encoding encoding = Encoding.UTF8;

			try
			{
				encoding = Encoding.GetEncoding(Properties.Settings.Default.ReceivedDataEncoding);
				this.Logger.LogInformation("Using text encoding {encoding}.", encoding);
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, "Exception while attempting to use encoding '{encoding}'.", Properties.Settings.Default.ReceivedDataEncoding);
			}

			//
			// Get the network stream.
			//
			this.Logger.LogDebug("Getting the network stream for communications.");
			NetworkStream stream = client.GetStream();

			//
			// Prepare a memory stream to read data into.
			//
			using (MemoryStream ms = new())
			{
				if (client.Connected && stream.CanRead)
				{
					this.Logger.LogInformation("The incoming connection is connected and can be read.");

					//
					// Set up a temporary buffer.
					//
					this.Logger.LogDebug("Creating buffer of 1024 bytes to read incoming data.");
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
					int numBytesRead = await stream.ReadAsync(data);
					ms.Write(data, 0, numBytesRead);
					this.Logger.LogInformation("{count} byte(s) were read from the incoming connection.", numBytesRead);

					while (stream.DataAvailable && numBytesRead > 0)
					{
						numBytesRead = await stream.ReadAsync(data);
						ms.Write(data, 0, numBytesRead);
						this.Logger.LogInformation("{count} additional byte(s) were read from the incoming connection.", numBytesRead);
					}
				}
				else
				{
					this.Logger.LogWarning("The incoming connection is not connected or cannot be read.");
				}

				//
				// Only process the request if data was received.
				//
				if (ms.Length > 0)
				{
					//
					// Get the request data.
					//
					this.Logger.LogInformation("{count} byte(s) total were received.", ms.Length);
					string requestData = encoding.GetString(ms.ToArray(), 0, (int)ms.Length);
					this.Logger.LogDebug("Incoming data: '{data}'.", requestData);

					//
					// Get the request handler.
					//
					IRequestHandler requestHandler = await this.RequestHandlerFactory.GetHandlerAsync(requestData);
					this.Logger.LogDebug("Using request handler '{handler}' to handle the incoming request.", requestHandler.GetType().Name);

					//
					//  Call the handler.
					//
					(bool closeConnection, string responseData) = (true, string.Empty);

					try
					{
						//
						// Get the handler for this request.
						//
						this.Logger.LogDebug("Calling {handler}.handleRequest().", requestHandler.GetType().Name);
						(closeConnection, responseData) = await requestHandler.HandleRequest(printerConfiguration, labelConfiguration, requestData);

						//
						// If the handler provided a response, send it back.
						//
						if (responseData != null)
						{
							this.Logger.LogDebug("Sending response data: '{data}'.", responseData);
							byte[] buffer = encoding.GetBytes(responseData);
							await stream.WriteAsync(buffer);
						}
						else
						{
							this.Logger.LogDebug("The handler did not return any response data.");
						}
					}
					catch (Exception ex)
					{
						this.Logger.LogError(ex, $"Exception occurred in {nameof(TcpListenerClientHandler)} while calling the request handler.");
					}
					finally
					{
						//
						// Close the connection if the handler indicated to do so.
						//
						if (closeConnection)
						{
							this.Logger.LogInformation("Closing the client connection.");

							if (stream != null)
							{
								stream.Close();
								stream.Dispose();
							}

							if (client != null)
							{
								client.Close();
								client.Dispose();
							}
						}
						else
						{
							this.Logger.LogInformation("Leaving the client connection open.");
						}
					}
				}
				else
				{
					this.Logger.LogWarning("There was no dat available on the incoming connection.");
				}
			}
		}
	}
}
