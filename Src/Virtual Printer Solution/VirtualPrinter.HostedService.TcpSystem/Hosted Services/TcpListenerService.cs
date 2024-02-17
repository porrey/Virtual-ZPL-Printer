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
using System.Net;
using System.Net.Sockets;
using Diamond.Core.Extensions.Hosting;
using Labelary.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prism.Events;
using VirtualPrinter.Db.Abstractions;
using VirtualPrinter.PublishSubscribe;

namespace VirtualPrinter.HostedService.TcpSystem
{
	public class TcpListenerService : HostedServiceTemplate
	{
		public TcpListenerService(ILogger<TcpListenerService> logger, IHostApplicationLifetime hostApplicationLifetime, IEventAggregator eventAggregator, IServiceScopeFactory serviceScopeFactory)
			: base(hostApplicationLifetime, logger, serviceScopeFactory)
		{
			this.EventAggregator = eventAggregator;
			this.ServiceScopeFactory = serviceScopeFactory;

			_ = this.EventAggregator.GetEvent<StartEvent>().Subscribe(async (e) =>
			  {
				  this.PrinterConfiguration = e.PrinterConfiguration;
				  this.LabelConfiguration = e.LabelConfiguration;
				  this.IpAddress = e.IpAddress;
				  this.Port = e.Port;
				  this.ImagePathRoot = e.ImagePath;
				  _ = await this.StartListenerAsync();
			  }, ThreadOption.BackgroundThread);

			_ = this.EventAggregator.GetEvent<StopEvent>().Subscribe(async (e) =>
			  {
				  await this.StopListenerAsync();
				  this.PrinterConfiguration = null;
				  this.LabelConfiguration = null;
				  this.ImagePathRoot = null;
				  this.Port = 0;
				  this.EventAggregator.GetEvent<RunningStateChangedEvent>().Publish(new RunningStateChangedEventArgs() { IsRunning = false });
			  }, ThreadOption.BackgroundThread);
		}

		protected IEventAggregator EventAggregator { get; set; }

		protected bool IsRunning { get; set; }
		protected IPrinterConfiguration PrinterConfiguration { get; set; }
		protected LabelConfiguration LabelConfiguration { get; set; }
		protected IPAddress IpAddress { get; set; }
		protected int Port { get; set; }
		protected TcpListener Listener { get; set; }
		protected ManualResetEvent ResetEvent { get; } = new(false);
		protected string ImagePathRoot { get; set; }
		protected CancellationTokenSource SocketCancellationTokenSource { get; set; }

		protected override void OnStarted()
		{
			_ = Task.Factory.StartNew(async () =>
			  {
				  using (IServiceScope scope = this.ServiceScopeFactory.CreateScope())
				  {
					  //
					  // Loop until the application is stopped.
					  //
					  while (!this.CancellationToken.IsCancellationRequested)
					  {
						  try
						  {
							  //
							  // Hold here until enabled.
							  //
							  if (this.ResetEvent.WaitOne())
							  {
								  try
								  {
									  //
									  // Accept the connection.
									  //
									  this.Logger.LogInformation("Waiting for incoming requests...");
									  TcpClient tcpClient = await this.Listener.AcceptTcpClientAsync(this.SocketCancellationTokenSource.Token);

									  if (!this.SocketCancellationTokenSource.IsCancellationRequested)
									  {
										  this.Logger.LogInformation("Incoming requests received.");

										  //
										  // Start the client.
										  //
										  TcpListenerClientHandler clientService = scope.ServiceProvider.GetRequiredService<TcpListenerClientHandler>();
										  this.Logger.LogInformation("Handing request to client service.");
										  _ = clientService.StartSessionAsync(tcpClient, this.PrinterConfiguration, this.LabelConfiguration);
									  }
								  }
								  catch (TaskCanceledException ex1)
								  {
									  this.Logger.LogError(ex1, nameof(TaskCanceledException));
								  }
								  catch (OperationCanceledException ex2)
								  {
									  this.Logger.LogError(ex2, nameof(OperationCanceledException));
								  }
								  catch (SocketException ex3)
								  {
									  this.Logger.LogError(ex3, nameof(SocketException));
								  }
								  catch (InvalidOperationException ex4)
								  {
									  this.Logger.LogError(ex4, nameof(InvalidOperationException));
								  }
							  }
						  }
						  catch (Exception ex)
						  {
							  this.Logger.LogError(ex, "Exception in TCP listener.");
							  this.EventAggregator.GetEvent<RunningStateChangedEvent>().Publish(new RunningStateChangedEventArgs() { IsRunning = false, IsError = true, ErrorMessage = ex.Message });
						  }
					  }
				  }
			  });
		}

		protected override async Task OnBeginStopAsync()
		{
			if (this.IsRunning)
			{
				this.Logger.LogInformation("Shutting down the TCP listener.");
				await this.StopListenerAsync();
			}
		}

		protected Task<bool> StartListenerAsync()
		{
			bool returnValue = false;

			try
			{
				this.Logger.LogInformation("Starting TCP listener.");
				this.SocketCancellationTokenSource = new();
				this.Listener = new TcpListener(this.IpAddress, this.Port);
				this.Listener.Start();
				_ = this.ResetEvent.Set();
				this.EventAggregator.GetEvent<RunningStateChangedEvent>().Publish(new RunningStateChangedEventArgs() { PrinterConfiguration = this.PrinterConfiguration, IsRunning = true });
				this.IsRunning = true;
				this.Logger.LogInformation("TCP listener was started successfully.");

				returnValue = true;
			}
			catch (SocketException socketEx)
			{
				this.Logger.LogError(socketEx, "Socket Exception");
				string message = socketEx.Message;

				if (socketEx.SocketErrorCode == SocketError.AddressAlreadyInUse)
				{
					message = "Address/port already in use.";
				}

				this.EventAggregator.GetEvent<RunningStateChangedEvent>().Publish(new RunningStateChangedEventArgs() { PrinterConfiguration = this.PrinterConfiguration, IsRunning = false, IsError = true, ErrorMessage = message });
				_ = this.ResetEvent.Reset();
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, "Exception while starting TCP listener.");
				this.EventAggregator.GetEvent<RunningStateChangedEvent>().Publish(new RunningStateChangedEventArgs() { PrinterConfiguration = this.PrinterConfiguration, IsRunning = false, IsError = true, ErrorMessage = ex.Message });
				_ = this.ResetEvent.Reset();
			}

			return Task.FromResult(returnValue);
		}

		protected async Task<bool> StopListenerAsync()
		{
			bool returnValue = false;

			try
			{
				this.Logger.LogDebug("Calling Cancel() on the cancellation token to stop the listener.");
				await this.SocketCancellationTokenSource.CancelAsync();
				//await this.ZplClient.SendStringAsync(this.IpAddress, this.Port, "NOP");
				this.Logger.LogDebug("Calling Stop() on the listener.");
				this.Listener.Stop();
				this.Logger.LogDebug("Raising the Running State Changed Event.");
				this.EventAggregator.GetEvent<RunningStateChangedEvent>().Publish(new RunningStateChangedEventArgs() { PrinterConfiguration = this.PrinterConfiguration, IsRunning = false });
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, "Exception in {name}().", nameof(StopListenerAsync));
				this.Logger.LogDebug("Raising the Running State Changed Event.");
				this.EventAggregator.GetEvent<RunningStateChangedEvent>().Publish(new RunningStateChangedEventArgs() { PrinterConfiguration = this.PrinterConfiguration, IsRunning = false, IsError = true, ErrorMessage = ex.Message });
				returnValue = false;
			}
			finally
			{
				_ = this.ResetEvent.Reset();
				this.Listener = null;
				this.SocketCancellationTokenSource = null;
				this.IsRunning = false;
			}

			return returnValue;
		}
	}
}
