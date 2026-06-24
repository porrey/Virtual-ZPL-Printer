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
using System.Collections.Specialized;
using System.Net;
using System.Text;
using Diamond.Core.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Labelary.Abstractions;
using VirtualPrinter.GrfStorageService;
using VirtualPrinter.PublishSubscribe;
using VirtualPrinter.ZplFormatService;

namespace VirtualPrinter.HostedService.HttpSystem
{
	// Implements the Zebra printer HTTP web server endpoints used by ZebraLabelUpdate:
	//   GET /printer/dir  — lists stored .ZPL format files
	//   GET /printer/zpl  — returns the content of a specific stored format
	//
	// ZebraLabelUpdate connects to http://<hostname>:<HttpPort>/ to read and edit
	// formats; the Save button writes back via TCP port 9100 (handled by ZplRequestHandler).
	//
	// HttpListener on http://localhost:<port>/ does not require admin or a netsh urlacl.
	// In ZebraLabelUpdate, type "localhost:9200" (or "127.0.0.1:9200") as the printer name.
	public class HttpListenerService : HostedServiceTemplate
	{
		public const int HttpPort = 9200;

		public HttpListenerService(ILogger<HttpListenerService> logger, IHostApplicationLifetime hostApplicationLifetime, IEventAggregator eventAggregator, IServiceScopeFactory serviceScopeFactory, IZplFormatService zplFormatService, IGrfStorageService grfStorageService, ILabelService labelService)
			: base(hostApplicationLifetime, logger, serviceScopeFactory)
		{
			this.EventAggregator   = eventAggregator;
			this.ZplFormatService  = zplFormatService;
			this.GrfStorageService = grfStorageService;
			this.LabelService      = labelService;

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

				this.Logger.LogInformation("HTTP listener started. In ZebraLabelUpdate, use 'localhost:{port}' as the printer name.", HttpPort);

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
					await this.HandleZplAsync(context);
				}
				else if (path.Equals("/printer/grf", StringComparison.OrdinalIgnoreCase))
				{
					await this.HandleGrfAsync(context);
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

		private async Task HandlePrinterIndexAsync(HttpListenerContext context)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
			sb.AppendLine("<title>Virtual ZPL Printer — Flash Memory</title>");
			sb.AppendLine("<style>");
			sb.AppendLine("  body { font-family: monospace; margin: 2em; }");
			sb.AppendLine("  h2 { border-bottom: 1px solid #ccc; padding-bottom: 4px; }");
			sb.AppendLine("  table { border-collapse: collapse; width: 100%; margin-bottom: 2em; }");
			sb.AppendLine("  th, td { text-align: left; padding: 4px 12px; border-bottom: 1px solid #eee; }");
			sb.AppendLine("  th { background: #f4f4f4; }");
			sb.AppendLine("  .none { color: #999; font-style: italic; }");
			sb.AppendLine("</style></head><body>");
			sb.AppendLine("<h1>Virtual ZPL Printer — Flash Memory</h1>");

			// --- Formats (ZPL) ---
			sb.AppendLine("<h2>Formats (ZPL)</h2>");

			DirectoryInfo fmtDir = this.ZplFormatService.FormatDirectory;
			FileInfo[] fmtFiles  = fmtDir.Exists ? fmtDir.GetFiles() : [];

			if (fmtFiles.Length == 0)
			{
				sb.AppendLine("<p class='none'>No formats stored. Send a ^DF job to the printer to store one.</p>");
			}
			else
			{
				sb.AppendLine("<table><tr><th>Name</th><th>Size</th><th>Modified</th><th></th></tr>");

				foreach (FileInfo file in fmtFiles.OrderBy(f => f.Name))
				{
					int underscore = file.Name.IndexOf('_');
					if (underscore <= 0) continue;

					string dev  = file.Name[..underscore];
					string rest = file.Name[(underscore + 1)..];
					int dot     = rest.LastIndexOf('.');
					if (dot <= 0) continue;

					string oname       = rest[..dot];
					string otype       = rest[(dot + 1)..];
					string displayName = $"{dev}:{rest}";
					string viewHref    = $"/printer/zpl?dev={dev}&oname={oname}&otype={otype}";

					sb.AppendLine($"<tr><td>{displayName}</td><td>{file.Length:N0} B</td><td>{file.LastWriteTime:yyyy-MM-dd HH:mm:ss}</td><td><a href='{viewHref}'>View</a></td></tr>");
				}

				sb.AppendLine("</table>");
			}

			// --- Graphics (GRF) ---
			sb.AppendLine("<h2>Graphics (GRF)</h2>");

			DirectoryInfo grfDir = this.GrfStorageService.GrfDirectory;
			FileInfo[] grfFiles  = grfDir.Exists ? grfDir.GetFiles() : [];

			if (grfFiles.Length == 0)
			{
				sb.AppendLine("<p class='none'>No graphics stored. Send a ~DG job to the printer to store one.</p>");
			}
			else
			{
				sb.AppendLine("<table><tr><th>Name</th><th>Size</th><th>Modified</th><th></th></tr>");

				foreach (FileInfo file in grfFiles.OrderBy(f => f.Name))
				{
					int underscore = file.Name.IndexOf('_');
					if (underscore <= 0) continue;

					string dev         = file.Name[..underscore];
					string rest        = file.Name[(underscore + 1)..];
					string displayName = $"{dev}:{rest}";
					string viewHref    = $"/printer/grf?dev={dev}&filename={rest}";

					sb.AppendLine($"<tr><td>{displayName}</td><td>{file.Length:N0} B</td><td>{file.LastWriteTime:yyyy-MM-dd HH:mm:ss}</td><td><a href='{viewHref}'>View</a></td></tr>");
				}

				sb.AppendLine("</table>");
			}

			sb.AppendLine("</body></html>");

			await WriteResponseAsync(context, "text/html", sb.ToString());

			this.Logger.LogDebug("Responded to /printer index.");
		}

		private async Task HandleDirAsync(HttpListenerContext context)
		{
			// Build an HTML page listing stored .ZPL format files.
			//
			// ZebraLabelUpdate's Delphi parser looks for the text "zpl?dev=" in the
			// page, then extracts the anchor's inner text as the display name. The
			// inner text must end in ".zpl" (case-insensitive) to be added to the list.
			//
			// Format files on disk are stored as "E_NFRC_AL.ZPL" (device_filename).
			// We convert that back to "E:NFRC_AL.ZPL" for display and "E" / "NFRC_AL" / "ZPL"
			// for the query-string parameters.

			var sb = new StringBuilder();
			sb.AppendLine("<html><body>");
			sb.AppendLine("<h2>E: DIRECTORY</h2><ul>");

			DirectoryInfo dir = this.ZplFormatService.FormatDirectory;

			if (dir.Exists)
			{
				foreach (FileInfo file in dir.GetFiles())
				{
					string storedName = file.Name;

					int underscore = storedName.IndexOf('_');
					if (underscore <= 0) continue;

					string dev  = storedName[..underscore];
					string rest = storedName[(underscore + 1)..];   // "NFRC_AL.ZPL"
					int dot     = rest.LastIndexOf('.');
					if (dot <= 0) continue;

					string oname       = rest[..dot];               // "NFRC_AL"
					string otype       = rest[(dot + 1)..];         // "ZPL"
					string displayName = $"{dev}:{rest}";           // "E:NFRC_AL.ZPL"

					sb.AppendLine($"  <li><a href=\"zpl?dev={dev}&oname={oname}&otype={otype}\">{displayName}</a></li>");
				}
			}

			sb.AppendLine("</ul></body></html>");

			await WriteResponseAsync(context, "text/html", sb.ToString());

			this.Logger.LogInformation("Responded to /printer/dir.");
		}

		private async Task HandleZplAsync(HttpListenerContext context)
		{
			// Parse query string: ?dev=E&oname=NFRC_AL&otype=ZPL
			NameValueCollection qs = context.Request.QueryString;
			string dev   = qs["dev"]   ?? string.Empty;
			string oname = qs["oname"] ?? string.Empty;
			string otype = qs["otype"] ?? string.Empty;

			if (string.IsNullOrEmpty(dev) || string.IsNullOrEmpty(oname) || string.IsNullOrEmpty(otype))
			{
				context.Response.StatusCode = 400;
				context.Response.Close();
				return;
			}

			// Reconstruct the storage key: E_NFRC_AL.ZPL
			string key      = $"{dev}_{oname}.{otype}";
			string filePath = Path.Combine(this.ZplFormatService.FormatDirectory.FullName, key);

			if (!File.Exists(filePath))
			{
				this.Logger.LogWarning("HTTP /printer/zpl: format '{key}' not found.", key);
				context.Response.StatusCode = 404;
				context.Response.Close();
				return;
			}

			// ZebraLabelUpdate's getZIPL extracts content between ^XA and ^XZ, so
			// wrap the stored body in those markers before sending.
			string body = await File.ReadAllTextAsync(filePath);
			string zpl  = $"^XA\r\n{body.Trim()}\r\n^XZ";

			await WriteResponseAsync(context, "text/plain", zpl);

			this.Logger.LogInformation("Responded to /printer/zpl for '{dev}:{oname}.{otype}'.", dev, oname, otype);
		}

		private async Task HandleGrfAsync(HttpListenerContext context)
		{
			// Renders a stored GRF image by constructing minimal ZPL that inlines the
			// ~DG blob and references it with ^XG, then passes it to Labelary.
			//
			// Query string: ?dev=E&filename=NFRC.GRF

			NameValueCollection qs = context.Request.QueryString;
			string dev      = qs["dev"]      ?? string.Empty;
			string filename = qs["filename"] ?? string.Empty;

			if (string.IsNullOrEmpty(dev) || string.IsNullOrEmpty(filename))
			{
				context.Response.StatusCode = 400;
				context.Response.Close();
				return;
			}

			if (this.LabelConfiguration == null)
			{
				await WriteResponseAsync(context, "text/plain", "No printer is running. Start a virtual printer first.");
				return;
			}

			string key      = $"{dev}_{filename}";
			string filePath = Path.Combine(this.GrfStorageService.GrfDirectory.FullName, key);

			if (!File.Exists(filePath))
			{
				this.Logger.LogWarning("HTTP /printer/grf: GRF '{key}' not found.", key);
				context.Response.StatusCode = 404;
				context.Response.Close();
				return;
			}

			string grfBlob = await File.ReadAllTextAsync(filePath);

			// Construct minimal ZPL: inline the ~DG blob then recall it with ^XG.
			// ^FO0,0 positions the image at the top-left corner of the label.
			string zpl = $"^XA\r\n{grfBlob.Trim()}\r\n^FO0,0^XG{dev}:{filename}^FS\r\n^XZ";

			IGetLabelResponse response = await this.LabelService.GetLabelAsync(this.LabelConfiguration, zpl);

			if (!response.Result || response.Label == null || response.Label.Length == 0)
			{
				string error = response.Error ?? "Labelary returned no image.";
				this.Logger.LogWarning("HTTP /printer/grf: Labelary render failed for '{key}': {error}", key, error);
				await WriteResponseAsync(context, "text/plain", $"Render failed: {error}");
				return;
			}

			context.Response.ContentType     = "image/png";
			context.Response.ContentLength64 = response.Label.Length;
			context.Response.StatusCode      = 200;

			await context.Response.OutputStream.WriteAsync(response.Label);
			context.Response.Close();

			this.Logger.LogInformation("Rendered GRF '{dev}:{filename}' via Labelary.", dev, filename);
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
