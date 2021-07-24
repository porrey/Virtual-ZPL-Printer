using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Diamond.Core.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prism.Events;
using VirtualPrinter.Client;
using VirtualPrinter.Events;
using VirtualPrinter.Models;

namespace VirtualPrinter.HostedServices
{
	public class TcpListenerService : HostedServiceTemplate
	{
		public TcpListenerService(ILogger<TcpListenerService> logger, IHostApplicationLifetime hostApplicationLifetime, IEventAggregator eventAggregator, IServiceScopeFactory serviceScopeFactory)
			: base(hostApplicationLifetime, logger, serviceScopeFactory)
		{
			this.EventAggregator = eventAggregator;
			this.ServiceScopeFactory = serviceScopeFactory;

			this.EventAggregator.GetEvent<StartEvent>().Subscribe(async (e) =>
			{
				this.LabelConfiguration = e.LabelConfiguration;
				this.Port = e.Port;
				this.ImagePathRoot = e.ImagePath;
				await this.StartListener();
			}, ThreadOption.BackgroundThread);

			this.EventAggregator.GetEvent<StopEvent>().Subscribe(async (e) =>
			{
				this.LabelConfiguration = null;
				this.Port = 0;
				this.ImagePathRoot = null;
				await this.StopListener();
				this.EventAggregator.GetEvent<RunningStateChangedEvent>().Publish(new RunningStateChangedEventArgs() { IsRunning = false });
			}, ThreadOption.BackgroundThread);
		}

		protected IEventAggregator EventAggregator { get; set; }
		protected bool IsRunning { get; set; } = false;
		protected LabelConfiguration LabelConfiguration { get; set; }
		protected int Port { get; set; }
		protected TcpListener Listener { get; set; }
		protected ManualResetEvent ResetEvent { get; } = new(false);
		protected string ImagePathRoot { get; set; }

		protected override void OnStarted()
		{
			Task.Factory.StartNew(async () =>
			{
				while (!this.CancellationToken.IsCancellationRequested)
				{
					try
					{
						if (this.ResetEvent.WaitOne())
						{
							try
							{
								//
								// Accept the connection.
								//
								TcpClient tcpClient = await this.Listener.AcceptTcpClientAsync();
								tcpClient.ReceiveTimeout = 1000;
								tcpClient.SendTimeout = 1000;
								tcpClient.LingerState = new LingerOption(false, 1);
								tcpClient.NoDelay = true;
								tcpClient.ReceiveBufferSize = 8192;
								tcpClient.SendBufferSize = 8192;

								using (IServiceScope scope = this.ServiceScopeFactory.CreateScope())
								{
									TcpListenerClientHandler clientService = scope.ServiceProvider.GetRequiredService<TcpListenerClientHandler>();
									await clientService.StartSessionAsync(tcpClient, this.LabelConfiguration, this.ImagePathRoot);
								}
							}
							catch (SocketException)
							{
								//
								// Happens when the socket is closed while listening.
								//
							}
						}
					}
					catch (Exception ex)
					{
						this.EventAggregator.GetEvent<RunningStateChangedEvent>().Publish(new RunningStateChangedEventArgs() { IsRunning = false, IsError = true, ErrorMessage = ex.Message });
					}
				}
			});
		}

		protected Task<bool> StartListener()
		{
			bool returnValue = false;

			try
			{
				this.Listener = TcpListener.Create(this.Port);
				this.Listener.Start();
				returnValue = true;
				this.ResetEvent.Set();
				this.EventAggregator.GetEvent<RunningStateChangedEvent>().Publish(new RunningStateChangedEventArgs() { IsRunning = true });
			}
			catch (Exception ex)
			{
				this.EventAggregator.GetEvent<RunningStateChangedEvent>().Publish(new RunningStateChangedEventArgs() { IsRunning = false, IsError = true, ErrorMessage = ex.Message });
				this.ResetEvent.Reset();
				returnValue = false;
			}

			return Task.FromResult(returnValue);
		}

		protected Task<bool> StopListener()
		{
			bool returnValue = false;

			try
			{
				this.Listener.Stop();
				this.Listener = null;
				this.EventAggregator.GetEvent<RunningStateChangedEvent>().Publish(new RunningStateChangedEventArgs() { IsRunning = false });
			}
			catch (Exception ex)
			{
				this.EventAggregator.GetEvent<RunningStateChangedEvent>().Publish(new RunningStateChangedEventArgs() { IsRunning = false, IsError = true, ErrorMessage = ex.Message });
				returnValue = false;
			}
			finally
			{
				this.ResetEvent.Reset();
			}

			return Task.FromResult(returnValue);
		}
	}
}
