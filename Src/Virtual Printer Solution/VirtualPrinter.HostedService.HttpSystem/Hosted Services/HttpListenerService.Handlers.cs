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
using System.Text.Json;
using Labelary.Abstractions;
using Microsoft.Extensions.Logging;

namespace VirtualPrinter.HostedService.HttpSystem
{
	public partial class HttpListenerService
	{
		private async Task HandlePrinterIndexAsync(HttpListenerContext context)
		{
			NameValueCollection qs = context.Request.QueryString;
			string statusParam     = qs["status"] ?? string.Empty;
			string statusFile      = qs["file"]   ?? string.Empty;
			string statusMsg       = qs["msg"]    ?? string.Empty;

			string statusBanner = string.Empty;
			if (statusParam.Equals("ok", StringComparison.OrdinalIgnoreCase))
				statusBanner = $"<div class='msg ok'>Uploaded <strong>{WebUtility.HtmlEncode(statusFile)}</strong> successfully.</div>";
			else if (statusParam.Equals("err", StringComparison.OrdinalIgnoreCase))
				statusBanner = $"<div class='msg err'>Upload failed: {WebUtility.HtmlEncode(statusMsg)}</div>";

			string formatTable = BuildFormatTable(this.ZplFormatService.FormatDirectory);
			string grfTable    = BuildGrfTable(this.GrfStorageService.GrfDirectory);

			string html = HttpResponseTemplates.BuildIndexPage(statusBanner, formatTable, grfTable);
			await WriteResponseAsync(context, "text/html", html);

			this.Logger.LogDebug("Responded to /printer index.");
		}

		private static string BuildFormatTable(DirectoryInfo fmtDir)
		{
			FileInfo[] files = fmtDir.Exists ? fmtDir.GetFiles() : [];

			if (files.Length == 0)
				return "<p class='none'>No formats stored. Send a ^DF job to the printer to store one.</p>";

			var sb = new StringBuilder();
			sb.AppendLine("<table><tr><th>Name</th><th>Size</th><th>Modified</th><th></th></tr>");

			foreach (FileInfo file in files.OrderBy(f => f.Name))
			{
				if (file.Name.EndsWith(".meta.json", StringComparison.OrdinalIgnoreCase)) continue;
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

				string deleteForm = $"<form method='post' action='/printer/delete' style='display:inline'><input type='hidden' name='key' value='{file.Name}'><button type='submit' onclick='return confirm(\"Delete {displayName}? This cannot be undone.\")'>Delete</button></form>";
				sb.AppendLine($"<tr><td>{displayName}</td><td>{file.Length:N0} B</td><td>{file.LastWriteTime:yyyy-MM-dd HH:mm:ss}</td><td><a href='{viewHref}'>View</a>&nbsp;&nbsp;{deleteForm}</td></tr>");
			}

			sb.AppendLine("</table>");
			return sb.ToString();
		}

		private static string BuildGrfTable(DirectoryInfo grfDir)
		{
			FileInfo[] files = grfDir.Exists ? grfDir.GetFiles() : [];

			if (files.Length == 0)
				return "<p class='none'>No graphics stored. Send a ~DG job to the printer to store one.</p>";

			var sb = new StringBuilder();
			sb.AppendLine("<table><tr><th>Name</th><th>Size</th><th>Modified</th><th></th></tr>");

			foreach (FileInfo file in files.OrderBy(f => f.Name))
			{
				int underscore = file.Name.IndexOf('_');
				if (underscore <= 0) continue;

				string dev         = file.Name[..underscore];
				string rest        = file.Name[(underscore + 1)..];
				string displayName = $"{dev}:{rest}";
				string viewHref    = $"/printer/grf?dev={dev}&filename={rest}";

				string deleteForm = $"<form method='post' action='/printer/delete' style='display:inline'><input type='hidden' name='key' value='{file.Name}'><button type='submit' onclick='return confirm(\"Delete {displayName}? This cannot be undone.\")'>Delete</button></form>";
				sb.AppendLine($"<tr><td>{displayName}</td><td>{file.Length:N0} B</td><td>{file.LastWriteTime:yyyy-MM-dd HH:mm:ss}</td><td><a href='{viewHref}'>View</a>&nbsp;&nbsp;{deleteForm}</td></tr>");
			}

			sb.AppendLine("</table>");
			return sb.ToString();
		}

		private async Task HandleDirAsync(HttpListenerContext context)
		{
			DirectoryInfo dir = this.ZplFormatService.FormatDirectory;
			var sb = new StringBuilder();

			if (dir.Exists)
			{
				foreach (FileInfo file in dir.GetFiles())
				{
					string storedName = file.Name;
					int underscore    = storedName.IndexOf('_');
					if (underscore <= 0) continue;

					string dev  = storedName[..underscore];
					string rest = storedName[(underscore + 1)..];
					int dot     = rest.LastIndexOf('.');
					if (dot <= 0) continue;

					string oname       = rest[..dot];
					string otype       = rest[(dot + 1)..];
					string displayName = $"{dev}:{rest}";

					sb.AppendLine($"  <li><a href=\"zpl?dev={dev}&oname={oname}&otype={otype}\">{displayName}</a></li>");
				}
			}

			string html = HttpResponseTemplates.BuildDirPage(sb.ToString());
			await WriteResponseAsync(context, "text/html", html);

			this.Logger.LogInformation("Responded to /printer/dir.");
		}

		private async Task HandleZplAsync(HttpListenerContext context)
		{
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

			string key      = $"{dev}_{oname}.{otype}";
			string filePath = Path.Combine(this.ZplFormatService.FormatDirectory.FullName, key);

			if (!File.Exists(filePath))
			{
				this.Logger.LogWarning("HTTP /printer/zpl: format '{key}' not found.", key);
				context.Response.StatusCode = 404;
				context.Response.Close();
				return;
			}

			string fileContent = await File.ReadAllTextAsync(filePath);
			string displayName = $"{dev}:{oname}.{otype}";
			string statusParam = qs["status"] ?? string.Empty;

			string metaPath = Path.Combine(this.ZplFormatService.FormatDirectory.FullName, $"{key}.meta.json");
			string metaJson = File.Exists(metaPath) ? await File.ReadAllTextAsync(metaPath) : "{}";

			string pageTitle     = $"Edit ZPL Script -- {WebUtility.HtmlEncode(displayName)}";
			string displayNameHtml = WebUtility.HtmlEncode(displayName);

			string statusBanner = string.Empty;
			if (statusParam.Equals("ok", StringComparison.OrdinalIgnoreCase))
				statusBanner = "<div class='msg ok'>Saved successfully.</div>";
			else if (statusParam.Equals("err", StringComparison.OrdinalIgnoreCase))
				statusBanner = $"<div class='msg err'>Save failed: {WebUtility.HtmlEncode(qs["msg"] ?? string.Empty)}</div>";

			string jsVars = string.Join("\n",
				$"var dev = {JsonSerializer.Serialize(dev)};",
				$"var oname = {JsonSerializer.Serialize(oname)};",
				$"var otype = {JsonSerializer.Serialize(otype)};",
				$"var originalContent = {JsonSerializer.Serialize(fileContent)};",
				$"var savedMeta = {metaJson};");

			string html = HttpResponseTemplates.BuildZplEditorPage(
				pageTitle,
				displayNameHtml,
				statusBanner,
				WebUtility.HtmlEncode(fileContent),
				jsVars);

			await WriteResponseAsync(context, "text/html", html);
			this.Logger.LogInformation("Responded to /printer/zpl edit page for '{dev}:{oname}.{otype}'.", dev, oname, otype);
		}

		private async Task HandleZplSaveAsync(HttpListenerContext context)
		{
			using MemoryStream ms = new();
			await context.Request.InputStream.CopyToAsync(ms);
			string body = Encoding.UTF8.GetString(ms.ToArray());

			string dev = null, oname = null, otype = null, content = null;

			foreach (string pair in body.Split('&'))
			{
				int eq = pair.IndexOf('=');
				if (eq < 0) continue;
				string name  = Uri.UnescapeDataString(pair[..eq].Replace('+', ' '));
				string value = Uri.UnescapeDataString(pair[(eq + 1)..].Replace('+', ' '));
				switch (name.ToLowerInvariant())
				{
					case "dev":     dev     = value; break;
					case "oname":   oname   = value; break;
					case "otype":   otype   = value; break;
					case "content": content = value; break;
				}
			}

			if (string.IsNullOrEmpty(dev) || string.IsNullOrEmpty(oname) || string.IsNullOrEmpty(otype) || content == null)
			{
				this.Redirect(context, $"/printer/zpl?dev={dev}&oname={oname}&otype={otype}&status=err&msg=Missing+parameters");
				return;
			}

			string key      = $"{dev}_{oname}.{otype}";
			string filePath = Path.Combine(this.ZplFormatService.FormatDirectory.FullName, key);

			if (!File.Exists(filePath))
			{
				this.Redirect(context, $"/printer/zpl?dev={dev}&oname={oname}&otype={otype}&status=err&msg=File+not+found");
				return;
			}

			await File.WriteAllTextAsync(filePath, content);
			this.Logger.LogInformation("Saved ZPL edit for '{dev}:{oname}.{otype}'.", dev, oname, otype);
			this.Redirect(context, $"/printer/zpl?dev={dev}&oname={oname}&otype={otype}&status=ok");
		}

		private async Task HandlePreviewAsync(HttpListenerContext context)
		{
			if (this.LabelConfiguration == null)
			{
				context.Response.StatusCode = 503;
				await WriteResponseAsync(context, "text/plain", "No printer running. Start a virtual printer first.");
				return;
			}

			using MemoryStream ms = new();
			await context.Request.InputStream.CopyToAsync(ms);
			string zplContent = Encoding.UTF8.GetString(ms.ToArray()).Trim();

			if (!zplContent.StartsWith("^XA", StringComparison.OrdinalIgnoreCase))
				zplContent = $"^XA\r\n{zplContent}\r\n^XZ";

			zplContent = await this.GrfStorageService.ApplyReferencedGrfAsync(zplContent);

			IGetLabelResponse response = await this.LabelService.GetLabelAsync(this.LabelConfiguration, zplContent);

			if (!response.Result || response.Label == null || response.Label.Length == 0)
			{
				string error = response.Error ?? "Labelary returned no image.";
				this.Logger.LogWarning("HTTP /printer/preview: Labelary render failed: {error}", error);
				context.Response.StatusCode = 502;
				await WriteResponseAsync(context, "text/plain", $"Render failed: {error}");
				return;
			}

			context.Response.ContentType     = "image/png";
			context.Response.ContentLength64 = response.Label.Length;
			context.Response.StatusCode      = 200;
			await context.Response.OutputStream.WriteAsync(response.Label);
			context.Response.Close();
			this.Logger.LogInformation("Rendered label preview via Labelary ({bytes} bytes).", response.Label.Length);
		}

		private async Task HandleZplMetaSaveAsync(HttpListenerContext context)
		{
			using MemoryStream ms = new();
			await context.Request.InputStream.CopyToAsync(ms);
			string body = Encoding.UTF8.GetString(ms.ToArray());

			string dev = null, oname = null, otype = null, meta = null;

			foreach (string pair in body.Split('&'))
			{
				int eq = pair.IndexOf('=');
				if (eq < 0) continue;
				string name  = Uri.UnescapeDataString(pair[..eq].Replace('+', ' '));
				string value = Uri.UnescapeDataString(pair[(eq + 1)..].Replace('+', ' '));
				switch (name.ToLowerInvariant())
				{
					case "dev":   dev   = value; break;
					case "oname": oname = value; break;
					case "otype": otype = value; break;
					case "meta":  meta  = value; break;
				}
			}

			if (string.IsNullOrEmpty(dev) || string.IsNullOrEmpty(oname) || string.IsNullOrEmpty(otype) || meta == null)
			{
				context.Response.StatusCode = 400;
				context.Response.Close();
				return;
			}

			string key      = $"{dev}_{oname}.{otype}";
			string metaPath = Path.Combine(this.ZplFormatService.FormatDirectory.FullName, $"{key}.meta.json");
			await File.WriteAllTextAsync(metaPath, meta);

			context.Response.StatusCode = 204;
			context.Response.Close();
			this.Logger.LogDebug("Saved FN meta for '{key}'.", key);
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

		private async Task HandleDeleteAsync(HttpListenerContext context)
		{
			// Read application/x-www-form-urlencoded body to get the storage key (e.g. "E_FILENAME.ZPL").
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

			// Also delete the sidecar meta file if it exists (Formats only).
			string metaSidecar = matched + ".meta.json";
			if (File.Exists(metaSidecar)) File.Delete(metaSidecar);

			this.Logger.LogInformation("Deleted flash file '{key}'.", key);

			string encodedKey = Uri.EscapeDataString(key);
			this.Redirect(context, $"/printer?status=ok&file={encodedKey}+deleted");
		}

		private async Task HandleGrfAsync(HttpListenerContext context)
		{
			// Renders a stored GRF image by constructing minimal ZPL that inlines the
			// ~DG blob and references it with ^XG, then passes it to Labelary.
			//
			// Query string: ?dev=E&filename=IMAGE.GRF

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

		private async Task HandleNewScriptAsync(HttpListenerContext context)
		{
			using MemoryStream ms = new();
			await context.Request.InputStream.CopyToAsync(ms);
			string body = Encoding.UTF8.GetString(ms.ToArray());

			string dev = null, name = null;

			foreach (string pair in body.Split('&'))
			{
				int eq = pair.IndexOf('=');
				if (eq < 0) continue;
				string pname  = Uri.UnescapeDataString(pair[..eq].Replace('+', ' '));
				string pvalue = Uri.UnescapeDataString(pair[(eq + 1)..].Replace('+', ' '));
				switch (pname.ToLowerInvariant())
				{
					case "dev":  dev  = pvalue; break;
					case "name": name = pvalue; break;
				}
			}

			if (string.IsNullOrWhiteSpace(dev) || string.IsNullOrWhiteSpace(name))
			{
				this.Redirect(context, "/printer?status=err&msg=Device+and+name+are+required");
				return;
			}

			// Sanitize: keep only alphanumeric + underscore, uppercase.
			name = new string(name.ToUpperInvariant().Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

			if (string.IsNullOrEmpty(name))
			{
				this.Redirect(context, "/printer?status=err&msg=Invalid+script+name");
				return;
			}

			string key      = $"{dev}_{name}.ZPL";
			string filePath = Path.Combine(this.ZplFormatService.FormatDirectory.FullName, key);

			if (File.Exists(filePath))
			{
				this.Redirect(context, $"/printer/zpl?dev={dev}&oname={name}&otype=ZPL");
				return;
			}

			this.ZplFormatService.FormatDirectory.Create();
			await File.WriteAllTextAsync(filePath, "^XA\r\n\r\n^XZ");

			this.Logger.LogInformation("Created new blank script '{dev}:{name}.ZPL'.", dev, name);
			this.Redirect(context, $"/printer/zpl?dev={dev}&oname={name}&otype=ZPL");
		}

		private async Task HandleImageUploadAsync(HttpListenerContext context)
		{
			// dev and name arrive as query-string params (set by the form's onsubmit);
			// the image file is in one of the multipart parts (name="file").
			NameValueCollection qs = context.Request.QueryString;
			string dev  = (qs["dev"]  ?? "E").ToUpperInvariant().Trim();
			string name = (qs["name"] ?? string.Empty).Trim();

			if (string.IsNullOrWhiteSpace(name))
			{
				this.Redirect(context, "/printer?status=err&msg=Image+name+is+required");
				return;
			}

			// Sanitize name: uppercase alphanumeric + underscore only.
			name = new string(name.ToUpperInvariant().Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

			string contentType = context.Request.ContentType ?? string.Empty;

			if (!contentType.Contains("multipart/form-data", StringComparison.OrdinalIgnoreCase))
			{
				this.Redirect(context, "/printer?status=err&msg=Expected+multipart/form-data");
				return;
			}

			string boundary = null;
			foreach (string ct in contentType.Split(';'))
			{
				string t = ct.Trim();
				if (t.StartsWith("boundary=", StringComparison.OrdinalIgnoreCase))
				{
					boundary = t["boundary=".Length..].Trim('"');
					break;
				}
			}

			if (string.IsNullOrEmpty(boundary))
			{
				this.Redirect(context, "/printer?status=err&msg=Missing+multipart+boundary");
				return;
			}

			// Read the full body as raw bytes — must not decode as text (binary image data).
			using MemoryStream ms = new();
			await context.Request.InputStream.CopyToAsync(ms);
			byte[] bodyBytes = ms.ToArray();

			byte[] partDelim  = Encoding.ASCII.GetBytes($"--{boundary}\r\n");
			byte[] headerSep  = "\r\n\r\n"u8.ToArray();
			byte[] partEnd    = Encoding.ASCII.GetBytes($"\r\n--{boundary}");

			// Walk every part until we find the one with name="file".
			byte[] imageBytes   = null;
			string origFilename = "image.png";
			int    searchFrom   = 0;

			while (imageBytes == null)
			{
				int partStart = IndexOf(bodyBytes, partDelim, searchFrom);
				if (partStart < 0) break;

				int headerStart = partStart + partDelim.Length;
				int headerEnd   = IndexOf(bodyBytes, headerSep, headerStart);
				if (headerEnd < 0) break;

				string headers     = Encoding.UTF8.GetString(bodyBytes, headerStart, headerEnd - headerStart);
				int    dataStart   = headerEnd + headerSep.Length;
				int    dataEnd     = IndexOf(bodyBytes, partEnd, dataStart);
				if (dataEnd < 0) dataEnd = bodyBytes.Length;

				searchFrom = dataEnd;

				if (!headers.Contains("name=\"file\"", StringComparison.OrdinalIgnoreCase))
					continue;

				imageBytes = bodyBytes[dataStart..dataEnd];

				foreach (string line in headers.Split('\n'))
				{
					if (!line.TrimStart().StartsWith("Content-Disposition", StringComparison.OrdinalIgnoreCase)) continue;
					foreach (string token in line.Split(';'))
					{
						string t = token.Trim();
						if (t.StartsWith("filename=", StringComparison.OrdinalIgnoreCase))
							origFilename = t["filename=".Length..].Trim('"', '\'', '\r', '\n');
					}
					break;
				}
			}

			if (imageBytes == null || imageBytes.Length == 0)
			{
				this.Redirect(context, "/printer?status=err&msg=Could+not+find+image+in+upload");
				return;
			}

			const int LabelaryMaxBytes = 200 * 1024;
			if (imageBytes.Length > LabelaryMaxBytes)
			{
				double kb = imageBytes.Length / 1024.0;
				string sizeMsg = Uri.EscapeDataString($"Image is {kb:F0} KB — Labelary limit is 200 KB. Please resize the image first.");
				this.Redirect(context, $"/printer?status=err&msg={sizeMsg}");
				return;
			}

			// Call Labelary to convert the image to a ZPL ~DG blob.
			string zplFromLabelary;
			using (MemoryStream imgStream = new(imageBytes))
				zplFromLabelary = await this.LabelService.ConvertImageToZplAsync(imgStream, origFilename);

			if (string.IsNullOrWhiteSpace(zplFromLabelary))
			{
				this.Redirect(context, $"/printer?status=err&msg=Labelary+image+conversion+failed+(check+app+log+for+details)");
				return;
			}

			// Labelary returns ^GFA (inline graphic field), not ~DG (flash storage).
			// ^GF format: ^GFA,<data-len>,<total-len>,<bytes-per-row>,<hex-data>^FS
			// ~DG format: ~DG{dev}:{name}.GRF,<data-len>,<bytes-per-row>,<hex-data>
			string grfFilename = $"{name}.GRF";
			var gfMatch = System.Text.RegularExpressions.Regex.Match(
				zplFromLabelary,
				@"\^GFA,(\d+),\d+,(\d+),([\s\S]+?)\^FS",
				System.Text.RegularExpressions.RegexOptions.IgnoreCase);

			if (!gfMatch.Success)
			{
				this.Logger.LogWarning("Labelary response did not contain ^GFA. Preview: {zpl}", zplFromLabelary[..Math.Min(200, zplFromLabelary.Length)]);
				this.Redirect(context, "/printer?status=err&msg=Could+not+parse+Labelary+GRF+response");
				return;
			}

			string dgContent = $"~DG{dev}:{grfFilename},{gfMatch.Groups[1].Value},{gfMatch.Groups[2].Value},{gfMatch.Groups[3].Value}";

			this.GrfStorageService.GrfDirectory.Create();
			await File.WriteAllTextAsync(
				Path.Combine(this.GrfStorageService.GrfDirectory.FullName, $"{dev}_{grfFilename}"),
				dgContent);

			this.Logger.LogInformation("Converted and stored image as GRF '{dev}:{grfFilename}'.", dev, grfFilename);
			this.Redirect(context, $"/printer?status=ok&file={Uri.EscapeDataString(grfFilename)}");
		}

		// Finds the first occurrence of 'pattern' bytes within 'source' starting at 'startIndex'.
		private static int IndexOf(byte[] source, byte[] pattern, int startIndex = 0)
		{
			for (int i = startIndex; i <= source.Length - pattern.Length; i++)
			{
				bool found = true;
				for (int j = 0; j < pattern.Length; j++)
				{
					if (source[i + j] != pattern[j]) { found = false; break; }
				}
				if (found) return i;
			}
			return -1;
		}
	}
}
