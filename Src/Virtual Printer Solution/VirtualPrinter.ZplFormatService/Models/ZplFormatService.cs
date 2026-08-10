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
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace VirtualPrinter.ZplFormatService
{
	internal class ZplFormatService(ILogger<ZplFormatService> logger, IMemoryCache memoryCache) : IZplFormatService
	{
		protected ILogger<ZplFormatService> Logger { get; } = logger;
		protected IMemoryCache MemoryCache { get; } = memoryCache;

		// ^DFE:FILENAME.ZPL^FS  — download format command
		private static readonly Regex DfPattern = new(
			@"\^DF(?<device>[A-Z]):(?<filename>[^\^]+)\^FS",
			RegexOptions.Compiled);

		// ^XFE:FILENAME.ZPL^FS  — recall format command.
		// Optionally captures the trailing ^FS so the Replace call removes it
		// along with the ^XF command (otherwise it's left as a stray ^FS after
		// the expanded template body).
		private static readonly Regex XfPattern = new(
			@"\^XF(?<device>[A-Z]):(?<filename>[^\^]+)(?:\^FS)?",
			RegexOptions.Compiled);


		public DirectoryInfo FormatDirectory => new(
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
						 "Virtual ZPL Printer", "Formats"));

		public async Task SaveFormatFromZplAsync(string zpl)
		{
			Match dfMatch = DfPattern.Match(zpl);

			if (!dfMatch.Success)
			{
				return;
			}

			this.FormatDirectory.Create();

			// Extract the format body: everything after the ^DF command up to ^XZ.
			// Strip the outer ^XA and ^XZ so we store only the template body —
			// the ^XA/^XZ of the print job itself will wrap the expanded body on recall.
			int bodyStart = dfMatch.Index + dfMatch.Length;
			int xzIndex = zpl.IndexOf("^XZ", bodyStart, StringComparison.OrdinalIgnoreCase);
			string body = xzIndex >= 0
							? zpl.Substring(bodyStart, xzIndex - bodyStart).Trim()
							: zpl.Substring(bodyStart).Trim();

			string device = dfMatch.Groups["device"].Value;
			string filename = dfMatch.Groups["filename"].Value.Trim();
			string key = $"{device}_{filename}";
			string path = Path.Combine(this.FormatDirectory.FullName, key);

			await File.WriteAllTextAsync(path, body);
			this.MemoryCache.Remove(key);

			this.Logger.LogInformation("Saved format '{device}:{filename}' to flash library.", device, filename);
		}

		public async Task<string> ApplyRecalledFormatsAsync(string zpl)
		{
			if (string.IsNullOrWhiteSpace(zpl) || !zpl.Contains("^XF"))
			{
				return zpl;
			}

			Match xfMatch = XfPattern.Match(zpl);

			if (!xfMatch.Success)
			{
				return zpl;
			}

			string device = xfMatch.Groups["device"].Value;
			string filename = xfMatch.Groups["filename"].Value.Trim();

			string templateBody = await this.GetFormatBodyAsync(device, filename);

			if (templateBody == null)
			{
				this.Logger.LogWarning("Format '{device}:{filename}' referenced via ^XF but not found in flash library.", device, filename);
				return zpl;
			}

			this.Logger.LogDebug("Prepending ^DF for '{device}:{filename}' to enable Labelary native recall.", device, filename);

			// Strip ^XA/^XZ wrapper — web-editor templates store the full label envelope;
			// ^DF-stored templates don't. Either way we want only the inner body.
			int xaIdx = templateBody.IndexOf("^XA", StringComparison.OrdinalIgnoreCase);
			if (xaIdx >= 0) templateBody = templateBody[(xaIdx + 3)..].TrimStart();
			int xzIdx = templateBody.LastIndexOf("^XZ", StringComparison.OrdinalIgnoreCase);
			if (xzIdx >= 0) templateBody = templateBody[..xzIdx].TrimEnd();

			// Collapse consecutive blank lines in both the template body and the incoming job.
			templateBody = Regex.Replace(templateBody, @"(\r?\n){2,}", "\r\n");
			string normalizedZpl = Regex.Replace(zpl.Trim(), @"(\r?\n){2,}", "\r\n");

			// Prepend a ^DF label so Labelary can resolve the ^XF natively — same as
			// sending a format-definition job before the print job on real hardware.
			// Labelary treats the ^DF label as non-printing, so the ^XF recall label
			// remains at index 0 in the response. ^FS is omitted after ^DF — it is not
			// required by ZPL and triggers an "Unnecessary ^FS" linter warning.
			return $"^XA\r\n^DF{device}:{filename}\r\n{templateBody}\r\n^XZ\r\n{normalizedZpl}";
		}

		public void InvalidateCache(string device, string filename)
		{
			this.MemoryCache.Remove($"{device}_{filename}");
		}

		private async Task<string> GetFormatBodyAsync(string device, string filename)
		{
			string key = $"{device}_{filename}";

			if (this.MemoryCache.TryGetValue(key, out string cached))
			{
				return cached;
			}

			string path = Path.Combine(this.FormatDirectory.FullName, key);

			if (!File.Exists(path))
			{
				return null;
			}

			string body = await File.ReadAllTextAsync(path);

			this.MemoryCache.Set(key, body, new MemoryCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
				SlidingExpiration = TimeSpan.FromMinutes(15)
			});

			return body;
		}
	}
}
