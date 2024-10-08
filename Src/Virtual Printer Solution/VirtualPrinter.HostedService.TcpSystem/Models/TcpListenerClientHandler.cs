﻿/*
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
using System.Net.Sockets;
using System.Security.RightsManagement;
using System.Text;
using ImageCache.Abstractions;
using Labelary.Abstractions;
using Microsoft.Extensions.Logging;
using Prism.Events;
using VirtualPrinter.ApplicationSettings;
using VirtualPrinter.Db.Abstractions;
using VirtualPrinter.Handler.Abstractions;

namespace VirtualPrinter.HostedService.TcpSystem
{
	public class TcpListenerClientHandler(ILogger<TcpListenerClientHandler> logger, IEventAggregator eventAggregator, ISettings settings, IRequestHandlerFactory requestHandlerFactory, ILabelService labelService, IImageCacheRepository imageCacheRepository)
	{
		protected ILogger<TcpListenerClientHandler> Logger { get; set; } = logger;
		protected IEventAggregator EventAggregator { get; set; } = eventAggregator;
		protected ISettings Settings { get; set; } = settings;
		protected IRequestHandlerFactory RequestHandlerFactory { get; set; } = requestHandlerFactory;
		protected ILabelService LabelService { get; set; } = labelService;
		protected IImageCacheRepository ImageCacheRepository { get; set; } = imageCacheRepository;

		public event EventHandler OnCompleted = null;

		public async Task StartSessionAsync(TcpClient client, IPrinterConfiguration printerConfiguration, ILabelConfiguration labelConfiguration)
		{
			this.Logger.LogInformation("Handling incoming request from {endpoint}.", client.Client.LocalEndPoint);

			//
			// Set parameters.
			//
			client.ReceiveTimeout = this.Settings.ReceiveTimeout;
			client.SendTimeout = this.Settings.SendTimeout;
			client.NoDelay = this.Settings.NoDelay;
			client.ReceiveBufferSize = this.Settings.ReceiveBufferSize;
			client.SendBufferSize = this.Settings.SendBufferSize;
			client.LingerState = new LingerOption(this.Settings.Linger, this.Settings.LingerTime);

			//
			// Use user-specified encoding in order to display special characters correctly.
			//
			Encoding encoding = Encoding.UTF8;

			try
			{
				encoding = Encoding.GetEncoding(this.Settings.ReceivedDataEncoding);
				this.Logger.LogInformation("Using text encoding {encoding}.", encoding);
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, "Exception while attempting to use encoding '{encoding}'. Falling back to UTF-8", this.Settings.ReceivedDataEncoding);
			}

			//
			// Get the network stream.
			//
			this.Logger.LogDebug("Getting the network stream for communications.");
			using (NetworkStream networkStream = client.GetStream())
			{
				//
				// Prepare a memory stream to read data into.
				//
				using (MemoryStream ms = new())
				{
					if (client.Connected && networkStream.CanRead)
					{
						this.Logger.LogInformation("The incoming connection is connected and can be read.");

						//
						// Set up a temporary buffer.
						//
						int bufferSize = client.ReceiveBufferSize == -1 ? 1024 : client.ReceiveBufferSize;
						this.Logger.LogDebug("Creating buffer of {size} bytes to read incoming data.", bufferSize);
						byte[] data = new byte[bufferSize];

						//
						// Create time stamp and cancellation token.
						//
						DateTime timestamp = DateTime.Now;
						CancellationTokenSource tokenSource = new();

						while ((networkStream.DataAvailable && networkStream.CanRead) || !tokenSource.Token.IsCancellationRequested)
						{
							//
							// Read available data.
							//
							int numBytesRead = await networkStream.ReadAsync(data, tokenSource.Token);
							string requestData = encoding.GetString(data);
							this.Logger.LogDebug("Data received: '{data}'.", requestData);

							//
							// Add the new data to the memory stream.
							//
							ms.Write(data, 0, numBytesRead);

							if (numBytesRead > 0)
							{
								//
								// Reset the timestamp if data has been received.
								//
								timestamp = DateTime.Now;
							}
							else
							{
								//
								// Check if we are past the wait time.
								//
								if (DateTime.Now.Subtract(timestamp) > TimeSpan.FromMilliseconds(this.Settings.MaximumWaitTime))
								{
									tokenSource.Cancel();
								}
							}

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
								await networkStream.WriteAsync(buffer);
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

								if (networkStream != null)
								{
									networkStream.Close();
									networkStream.Dispose();
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
						this.Logger.LogWarning("There was no data available on the incoming connection.");
					}
				}
			}

			//
			// Fire the completion event.
			//
			this.OnCompleted?.Invoke(this, new EventArgs());
		}
	}
}
