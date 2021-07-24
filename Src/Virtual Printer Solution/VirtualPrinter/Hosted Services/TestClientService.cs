using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Diamond.Core.Extensions.Hosting;
using Microsoft.Extensions.Hosting;
using Prism.Events;
using VirtualPrinter.Events;

namespace VirtualPrinter.HostedServices
{
	public class TestClientService : HostedServiceTemplate
	{
		public TestClientService(IHostApplicationLifetime hostApplicationLifetime, IEventAggregator eventAggregator)
			: base(hostApplicationLifetime)
		{
			this.EventAggregator = eventAggregator;
			this.Zpl = File.ReadAllText("./samples/zpl.txt");

			this.EventAggregator.GetEvent<StartEvent>().Subscribe((e) => { this.Port = e.Port; this.ResetEvent.Set(); }, ThreadOption.BackgroundThread);
			this.EventAggregator.GetEvent<StopEvent>().Subscribe((e) => this.ResetEvent.Reset(), ThreadOption.BackgroundThread);
		}

		static int Id = 1;
		protected int Port { get; set; }
		protected IEventAggregator EventAggregator { get; set; }
		protected string Zpl { get; set; }
		protected ManualResetEvent ResetEvent { get; } = new(false);

		protected override void OnStarted()
		{
			Task.Factory.StartNew(async () =>
			{
				while (!this.CancellationToken.IsCancellationRequested)
				{
					if (this.ResetEvent.WaitOne())
					{
						using (TcpClient client = new())
						{
							await client.ConnectAsync("127.0.0.1", this.Port);

							using (Stream stream = client.GetStream())
							{
								byte[] buffer = ASCIIEncoding.UTF8.GetBytes(this.Zpl.Replace("{id}", Id.ToString("00000000")));
								Id++;
								await stream.WriteAsync(buffer.AsMemory(0, buffer.Length));
								client.Close();
							}
						}

						await Task.Delay(6000);
					}
				}
			});
		}
	}
}
