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
using Microsoft.Extensions.Configuration;
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

		private async Task HandlePrinterIndexAsync(HttpListenerContext context)
		{
			// Pick up optional ?status=ok&file=X or ?status=err&msg=X set by the upload redirect.
			NameValueCollection qs    = context.Request.QueryString;
			string statusParam        = qs["status"] ?? string.Empty;
			string statusFile         = qs["file"]   ?? string.Empty;
			string statusMsg          = qs["msg"]    ?? string.Empty;

			var sb = new StringBuilder();
			sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
			sb.AppendLine("<title>Virtual ZPL Printer — Flash Memory</title>");
			sb.AppendLine("<style>");
			sb.AppendLine("  body { font-family: monospace; margin: 2em; }");
			sb.AppendLine("  h2 { border-bottom: 1px solid #ccc; padding-bottom: 4px; }");
			sb.AppendLine("  table { border-collapse: collapse; width: 100%; margin-bottom: 1em; }");
			sb.AppendLine("  th, td { text-align: left; padding: 4px 12px; border-bottom: 1px solid #eee; }");
			sb.AppendLine("  th { background: #f4f4f4; }");
			sb.AppendLine("  .none { color: #999; font-style: italic; }");
			sb.AppendLine("  .upload { margin-bottom: 2em; display: flex; align-items: center; gap: 8px; }");
			sb.AppendLine("  .upload input[type=file] { font-family: monospace; }");
			sb.AppendLine("  .upload button { padding: 3px 10px; cursor: pointer; }");
			sb.AppendLine("  .msg { padding: 6px 12px; border-radius: 4px; margin-bottom: 1em; }");
			sb.AppendLine("  .msg.ok  { background: #d4edda; color: #155724; }");
			sb.AppendLine("  .msg.err { background: #f8d7da; color: #721c24; }");
			sb.AppendLine("</style></head><body>");
			sb.AppendLine("<h1>Virtual ZPL Printer — Flash Memory</h1>");

			if (statusParam.Equals("ok", StringComparison.OrdinalIgnoreCase))
				sb.AppendLine($"<div class='msg ok'>Uploaded <strong>{System.Net.WebUtility.HtmlEncode(statusFile)}</strong> successfully.</div>");
			else if (statusParam.Equals("err", StringComparison.OrdinalIgnoreCase))
				sb.AppendLine($"<div class='msg err'>Upload failed: {System.Net.WebUtility.HtmlEncode(statusMsg)}</div>");

			// --- Single upload form (auto-detects ^DF vs ~DG) ---
			sb.AppendLine("<form class='upload' method='post' action='/printer/upload' enctype='multipart/form-data'>");
			sb.AppendLine("  <input type='file' name='file' accept='.zpl,.pl'>");
			sb.AppendLine("  <button type='submit'>Upload</button>");
			sb.AppendLine("  <small>Files with ^DF are stored as Formats; files with ~DG are stored as Graphics; files with neither are imported as a Format using the filename.</small>");
			sb.AppendLine("</form>");
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

					string deleteForm = $"<form method='post' action='/printer/delete' style='display:inline'><input type='hidden' name='key' value='{file.Name}'><button type='submit'>Delete</button></form>";
					sb.AppendLine($"<tr><td>{displayName}</td><td>{file.Length:N0} B</td><td>{file.LastWriteTime:yyyy-MM-dd HH:mm:ss}</td><td><a href='{viewHref}'>View</a>&nbsp;&nbsp;{deleteForm}</td></tr>");
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

					string deleteForm = $"<form method='post' action='/printer/delete' style='display:inline'><input type='hidden' name='key' value='{file.Name}'><button type='submit'>Delete</button></form>";
					sb.AppendLine($"<tr><td>{displayName}</td><td>{file.Length:N0} B</td><td>{file.LastWriteTime:yyyy-MM-dd HH:mm:ss}</td><td><a href='{viewHref}'>View</a>&nbsp;&nbsp;{deleteForm}</td></tr>");
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

		private async Task HandleUploadAsync(HttpListenerContext context)
		{
			// Parse multipart/form-data, extract the file content as ZPL text, then
			// run it through the same storage pipeline as a live ZPL print job.
			// Both ~DG (GRF) and ^DF (format template) are captured automatically.

			string contentType = context.Request.ContentType ?? string.Empty;

			if (!contentType.Contains("multipart/form-data", StringComparison.OrdinalIgnoreCase))
			{
				this.Redirect(context, "/printer?status=err&msg=Expected+multipart/form-data");
				return;
			}

			// Extract the boundary token from Content-Type.
			string boundary = null;

			foreach (string part in contentType.Split(';'))
			{
				string trimmed = part.Trim();

				if (trimmed.StartsWith("boundary=", StringComparison.OrdinalIgnoreCase))
				{
					boundary = trimmed["boundary=".Length..].Trim('"');
					break;
				}
			}

			if (string.IsNullOrEmpty(boundary))
			{
				this.Redirect(context, "/printer?status=err&msg=Missing+multipart+boundary");
				return;
			}

			// Read the full request body.
			using MemoryStream ms = new();
			await context.Request.InputStream.CopyToAsync(ms);
			string body = Encoding.UTF8.GetString(ms.ToArray());

			// Locate the file part: skip boundary + part headers, read until closing boundary.
			string partStart  = $"--{boundary}\r\n";
			string partEnd    = $"\r\n--{boundary}";
			int headerEnd     = body.IndexOf("\r\n\r\n", StringComparison.Ordinal);
			int contentStart  = headerEnd >= 0 ? headerEnd + 4 : 0;
			int contentEnd    = body.IndexOf(partEnd, contentStart, StringComparison.Ordinal);
			string zplContent = contentEnd >= 0
				? body[contentStart..contentEnd]
				: body[contentStart..];

			if (string.IsNullOrWhiteSpace(zplContent))
			{
				this.Redirect(context, "/printer?status=err&msg=Uploaded+file+was+empty");
				return;
			}

			// Extract the original filename from Content-Disposition for the success message.
			string filename = "file";

			foreach (string line in body[..contentStart].Split('\n'))
			{
				if (!line.TrimStart().StartsWith("Content-Disposition", StringComparison.OrdinalIgnoreCase)) continue;

				foreach (string token in line.Split(';'))
				{
					string t = token.Trim();

					if (t.StartsWith("filename=", StringComparison.OrdinalIgnoreCase))
					{
						filename = t["filename=".Length..].Trim('"', '\'', '\r', '\n');
						break;
					}
				}
			}

			bool hasDf = zplContent.Contains("^DF", StringComparison.OrdinalIgnoreCase);
			bool hasDg = zplContent.Contains("~DG", StringComparison.OrdinalIgnoreCase);

			if (!hasDf && !hasDg)
			{
				// No explicit ^DF or ~DG — treat the whole file as a format template and
				// wrap it automatically using the uploaded filename as the format name.
				string baseName = Path.GetFileNameWithoutExtension(filename);
				string ext      = Path.GetExtension(filename).TrimStart('.').ToUpperInvariant();
				if (string.IsNullOrEmpty(ext)) ext = "ZPL";
				string dfName   = $"E:{baseName}.{ext}";

				// Strip the outer ^XA / ^XZ so the inner content can be re-wrapped cleanly.
				string inner = zplContent;
				int xaIdx    = inner.IndexOf("^XA", StringComparison.OrdinalIgnoreCase);
				if (xaIdx >= 0) inner = inner[(xaIdx + 3)..];
				int xzIdx    = inner.LastIndexOf("^XZ", StringComparison.OrdinalIgnoreCase);
				if (xzIdx >= 0) inner = inner[..xzIdx];

				zplContent = $"^XA\r\n^DF{dfName}^FS\r\n{inner.Trim()}\r\n^XZ";

				this.Logger.LogInformation("No ^DF found in '{filename}' — auto-wrapped as {dfName}.", filename, dfName);
			}

			// Run through the standard flash-storage pipeline — same as a live ZPL job.
			await this.GrfStorageService.SaveGrfFromZplAsync(zplContent);
			await this.ZplFormatService.SaveFormatFromZplAsync(zplContent);

			this.Logger.LogInformation("Uploaded '{filename}' via HTTP.", filename);

			string encodedFile = Uri.EscapeDataString(filename);
			this.Redirect(context, $"/printer?status=ok&file={encodedFile}");
		}

		private void Redirect(HttpListenerContext context, string location)
		{
			context.Response.StatusCode        = 302;
			context.Response.RedirectLocation  = location;
			context.Response.Close();
		}

		private async Task HandleDeleteAsync(HttpListenerContext context)
		{
			// Read application/x-www-form-urlencoded body to get the storage key (e.g. "E_NFRC_AL.ZPL").
			using MemoryStream ms = new();
			await context.Request.InputStream.CopyToAsync(ms);
			string body = Encoding.UTF8.GetString(ms.ToArray());

			string key = null;

			foreach (string pair in body.Split('&'))
			{
				int eq = pair.IndexOf('=');
				if (eq < 0) continue;

				string name  = Uri.UnescapeDataString(pair[..eq].Replace('+', ' '));
				string value = Uri.UnescapeDataString(pair[(eq + 1)..].Replace('+', ' '));

				if (name.Equals("key", StringComparison.OrdinalIgnoreCase))
				{
					key = value;
					break;
				}
			}

			if (string.IsNullOrEmpty(key))
			{
				this.Redirect(context, "/printer?status=err&msg=Missing+key");
				return;
			}

			// Try Formats directory first, then Graphics.
			string[] candidates =
			[
				Path.Combine(this.ZplFormatService.FormatDirectory.FullName, key),
				Path.Combine(this.GrfStorageService.GrfDirectory.FullName,   key),
			];

			string matched = candidates.FirstOrDefault(File.Exists);

			if (matched == null)
			{
				this.Redirect(context, $"/printer?status=err&msg=File+not+found");
				return;
			}

			File.Delete(matched);
			this.Logger.LogInformation("Deleted flash file '{key}'.", key);

			string encodedKey = Uri.EscapeDataString(key);
			this.Redirect(context, $"/printer?status=ok&file={encodedKey}+deleted");
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
