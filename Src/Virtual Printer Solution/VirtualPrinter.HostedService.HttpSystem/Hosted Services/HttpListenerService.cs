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
using System.Text;
using Diamond.Core.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Labelary.Abstractions;
using Microsoft.Extensions.Configuration;
using VirtualPrinter.GrfStorageService;
using VirtualPrinter.PublishSubscribe;
using VirtualPrinter.ZplFormatService;

namespace VirtualPrinter.HostedService.HttpSystem
{
	// Implements the Zebra printer HTTP web server endpoints:
	//   GET /printer/dir  — lists stored .ZPL format files
	//   GET /printer/zpl  — returns the content of a specific stored format
	public partial class HttpListenerService : HostedServiceTemplate
	{
		public const int DefaultHttpPort = 9200;

		public HttpListenerService(ILogger<HttpListenerService> logger, IHostApplicationLifetime hostApplicationLifetime, IEventAggregator eventAggregator, IServiceScopeFactory serviceScopeFactory, IZplFormatService zplFormatService, IGrfStorageService grfStorageService, ILabelService labelService,
			IConfiguration configuration)
			: base(hostApplicationLifetime, logger, serviceScopeFactory)
		{
			this.EventAggregator   = eventAggregator;
			this.ZplFormatService  = zplFormatService;
			this.GrfStorageService = grfStorageService;
			this.LabelService      = labelService;
			this.HttpPort          = configuration.GetValue<int>("HttpSystem:Port", DefaultHttpPort);
			this.HttpPort          = configuration.GetValue<int>("HttpSystem:Port", DefaultHttpPort);

			_ = this.EventAggregator.GetEvent<StartEvent>().Subscribe(async (e) =>
			  {
				  this.LabelConfiguration = e.LabelConfiguration;
				  _ = await this.StartListenerAsync();
			  }, ThreadOption.BackgroundThread);

			_ = this.EventAggregator.GetEvent<StopEvent>().Subscribe(async (e) =>
			  {
				  await this.StopListenerAsync();
			  }, ThreadOption.BackgroundThread);
		}

		protected IEventAggregator EventAggregator { get; set; }
		protected IZplFormatService ZplFormatService { get; set; }
		protected IGrfStorageService GrfStorageService { get; set; }
		protected ILabelService LabelService { get; set; }
		protected ILabelConfiguration LabelConfiguration { get; set; }
		protected int HttpPort { get; }
		protected HttpListener Listener { get; set; }
		protected bool IsRunning { get; set; }
		protected CancellationTokenSource ListenerCts { get; set; }

		protected override void OnStarted()
		{
			// HTTP listener is started on StartEvent (when a virtual printer is activated),
			// not on application start — same pattern as TcpListenerService.
		}

		protected override async Task OnBeginStopAsync()
		{
			if (this.IsRunning)
			{
				await this.StopListenerAsync();
			}
		}

		private Task<bool> StartListenerAsync()
		{
			bool returnValue = false;

			try
			{
				if (this.IsRunning)
				{
					return Task.FromResult(true);
				}

				this.Logger.LogInformation("Starting HTTP listener on port {port}.", HttpPort);
				this.ListenerCts = new CancellationTokenSource();
				this.Listener    = new HttpListener();
				this.Listener.Prefixes.Add($"http://localhost:{HttpPort}/");
				this.Listener.Start();
				this.IsRunning = true;

				this.Logger.LogInformation("HTTP listener started. Use 'http://localhost:{port}/printer' to access.", HttpPort);

				_ = Task.Run(() => this.AcceptLoopAsync(this.ListenerCts.Token));

				returnValue = true;
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, "Failed to start HTTP listener on port {port}.", HttpPort);
			}

			return Task.FromResult(returnValue);
		}

		private async Task StopListenerAsync()
		{
			try
			{
				this.Logger.LogInformation("Stopping HTTP listener.");
				await this.ListenerCts.CancelAsync();
				this.Listener?.Stop();
				this.Listener?.Close();
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, "Exception while stopping HTTP listener.");
			}
			finally
			{
				this.Listener    = null;
				this.ListenerCts = null;
				this.IsRunning   = false;
			}
		}

		private async Task AcceptLoopAsync(CancellationToken cancellationToken)
		{
			while (this.Listener?.IsListening == true)
			{
				try
				{
					HttpListenerContext context = await this.Listener.GetContextAsync();
					_ = Task.Run(() => this.HandleRequestAsync(context), cancellationToken);
				}
				catch (HttpListenerException)
				{
					// Thrown when Listener.Stop() is called — expected shutdown path.
					break;
				}
				catch (ObjectDisposedException)
				{
					break;
				}
				catch (Exception ex)
				{
					this.Logger.LogError(ex, "Error in HTTP accept loop.");
				}
			}
		}

		private async Task HandleRequestAsync(HttpListenerContext context)
		{
			string path = context.Request.Url?.AbsolutePath ?? string.Empty;

			this.Logger.LogDebug("HTTP {method} {url}", context.Request.HttpMethod, context.Request.Url);

			try
			{
				if (path.Equals("/printer", StringComparison.OrdinalIgnoreCase) ||
				    path.Equals("/printer/", StringComparison.OrdinalIgnoreCase))
				{
					await this.HandlePrinterIndexAsync(context);
				}
				else if (path.Equals("/printer/dir", StringComparison.OrdinalIgnoreCase))
				{
					await this.HandleDirAsync(context);
				}
				else if (path.Equals("/printer/zpl", StringComparison.OrdinalIgnoreCase))
				{
					if (context.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
						await this.HandleZplSaveAsync(context);
					else
						await this.HandleZplAsync(context);
				}
				else if (path.Equals("/printer/preview", StringComparison.OrdinalIgnoreCase) &&
				         context.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
				{
					await this.HandlePreviewAsync(context);
				}
				else if (path.Equals("/printer/zpl/meta", StringComparison.OrdinalIgnoreCase) &&
				         context.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
				{
					await this.HandleZplMetaSaveAsync(context);
				}
				else if (path.Equals("/printer/grf", StringComparison.OrdinalIgnoreCase))
				{
					await this.HandleGrfAsync(context);
				}
				else if (path.Equals("/printer/upload", StringComparison.OrdinalIgnoreCase) &&
				         context.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
				{
					await this.HandleUploadAsync(context);
				}
				else if (path.Equals("/printer/delete", StringComparison.OrdinalIgnoreCase) &&
				         context.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
				{
					await this.HandleDeleteAsync(context);
				}
				else
				{
					context.Response.StatusCode = 404;
					context.Response.Close();
				}
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, "Error handling HTTP {method} {url}.", context.Request.HttpMethod, context.Request.Url);

				try
				{
					context.Response.StatusCode = 500;
					context.Response.Close();
				}
				catch { }
			}
		}

		private void Redirect(HttpListenerContext context, string location)
		{
			context.Response.StatusCode        = 302;
			context.Response.RedirectLocation  = location;
			context.Response.Close();
		}

		private static async Task WriteResponseAsync(HttpListenerContext context, string contentType, string body)
		{
			byte[] buffer = Encoding.UTF8.GetBytes(body);
			context.Response.ContentType     = contentType;
			context.Response.ContentLength64 = buffer.Length;
			context.Response.StatusCode      = 200;

			await context.Response.OutputStream.WriteAsync(buffer);
			context.Response.Close();
		}
	}
}