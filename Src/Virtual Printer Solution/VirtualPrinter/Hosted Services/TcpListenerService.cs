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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Diamond.Core.Extensions.Hosting;
using Labelary.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prism.Events;
using VirtualZplPrinter.Client;
using VirtualZplPrinter.Events;

namespace VirtualZplPrinter.HostedServices
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
				  this.LabelConfiguration = e.LabelConfiguration;
				  this.IpAddress = e.IpAddress;
				  this.Port = e.Port;
				  this.ImagePathRoot = e.ImagePath;
				  _ = await this.StartListener();
			  }, ThreadOption.BackgroundThread);

			_ = this.EventAggregator.GetEvent<StopEvent>().Subscribe(async (e) =>
			  {
				  this.LabelConfiguration = null;
				  this.Port = 0;
				  this.ImagePathRoot = null;
				  await this.StopListener();
				  this.EventAggregator.GetEvent<RunningStateChangedEvent>().Publish(new RunningStateChangedEventArgs() { IsRunning = false });
			  }, ThreadOption.BackgroundThread);
		}

		protected IEventAggregator EventAggregator { get; set; }
		protected bool IsRunning { get; set; }
		protected LabelConfiguration LabelConfiguration { get; set; }
		protected IPAddress IpAddress { get; set; }
		protected int Port { get; set; }
		protected TcpListener Listener { get; set; }
		protected ManualResetEvent ResetEvent { get; } = new(false);
		protected string ImagePathRoot { get; set; }

		protected override void OnStarted()
		{
			_ = Task.Factory.StartNew(async () =>
			  {
				  using (IServiceScope scope = this.ServiceScopeFactory.CreateScope())
				  {
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
									  TcpClient tcpClient = await this.Listener.AcceptTcpClientAsync();
									  
									  //
									  // Start the client.
									  //
									  TcpListenerClientHandler clientService = scope.ServiceProvider.GetRequiredService<TcpListenerClientHandler>();
									  _ = clientService.StartSessionAsync(tcpClient, this.LabelConfiguration, this.ImagePathRoot);
								  }
								  catch (SocketException)
								  {
								  }
								  catch (InvalidOperationException)
								  {
								  }
							  }
						  }
						  catch (Exception ex)
						  {
							  this.EventAggregator.GetEvent<RunningStateChangedEvent>().Publish(new RunningStateChangedEventArgs() { IsRunning = false, IsError = true, ErrorMessage = ex.Message });
						  }
					  }
				  }
			  });
		}

		protected Task<bool> StartListener()
		{
			bool returnValue = false;

			try
			{
				this.Listener = new TcpListener(this.IpAddress, this.Port);
				this.Listener.Start();
				_ = this.ResetEvent.Set();
				this.EventAggregator.GetEvent<RunningStateChangedEvent>().Publish(new RunningStateChangedEventArgs() { IsRunning = true });
				returnValue = true;
			}
			catch (SocketException socketEx)
			{
				string message = socketEx.Message;

				if (socketEx.SocketErrorCode == SocketError.AddressAlreadyInUse)
				{
					message = "Address/port already in use.";
				}

				this.EventAggregator.GetEvent<RunningStateChangedEvent>().Publish(new RunningStateChangedEventArgs() { IsRunning = false, IsError = true, ErrorMessage = message });
				_ = this.ResetEvent.Reset();
				returnValue = false;
			}
			catch (Exception ex)
			{
				this.EventAggregator.GetEvent<RunningStateChangedEvent>().Publish(new RunningStateChangedEventArgs() { IsRunning = false, IsError = true, ErrorMessage = ex.Message });
				_ = this.ResetEvent.Reset();
				returnValue = false;
			}

			return Task.FromResult(returnValue);
		}

		protected async Task<bool> StopListener()
		{
			bool returnValue = false;

			try
			{
				this.Listener.Stop();
				await Task.Delay(1);
				this.EventAggregator.GetEvent<RunningStateChangedEvent>().Publish(new RunningStateChangedEventArgs() { IsRunning = false });
			}
			catch (Exception ex)
			{
				this.EventAggregator.GetEvent<RunningStateChangedEvent>().Publish(new RunningStateChangedEventArgs() { IsRunning = false, IsError = true, ErrorMessage = ex.Message });
				returnValue = false;
			}
			finally
			{
				_ = this.ResetEvent.Reset();
				this.Listener = null;
			}

			return returnValue;
		}
	}
}
