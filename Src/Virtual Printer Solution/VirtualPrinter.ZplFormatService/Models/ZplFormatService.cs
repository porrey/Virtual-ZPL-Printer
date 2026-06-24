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

		// ^XFE:FILENAME.ZPL^FS  — recall format command
		private static readonly Regex XfPattern = new(
			@"\^XF(?<device>[A-Z]):(?<filename>[^\^]+)",
			RegexOptions.Compiled);

		// ^FN1^FDvalue^FS  — field data provided in a print job
		// Also handles ^FN1"hint"^FDvalue^FS
		private static readonly Regex FdPattern = new(
			@"\^FN(?<num>\d+)(?:""[^""]*"")?\^FD(?<value>.*?)\^FS",
			RegexOptions.Compiled);

		// ^FN1  or  ^FN1"hint"  — field-number placeholder inside a stored template
		private static readonly Regex FnPlaceholderPattern = new(
			@"\^FN(?<num>\d+)(?:""[^""]*"")?",
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
			int xzIndex   = zpl.IndexOf("^XZ", bodyStart, StringComparison.OrdinalIgnoreCase);
			string body   = xzIndex >= 0
							? zpl.Substring(bodyStart, xzIndex - bodyStart).Trim()
							: zpl.Substring(bodyStart).Trim();

			string device   = dfMatch.Groups["device"].Value;
			string filename = dfMatch.Groups["filename"].Value.Trim();
			string key      = $"{device}_{filename}";
			string path     = Path.Combine(this.FormatDirectory.FullName, key);

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

			string device   = xfMatch.Groups["device"].Value;
			string filename = xfMatch.Groups["filename"].Value.Trim();

			string templateBody = await this.GetFormatBodyAsync(device, filename);

			if (templateBody == null)
			{
				this.Logger.LogWarning("Format '{device}:{filename}' referenced via ^XF but not found in flash library.", device, filename);
				return zpl;
			}

			// Collect field values from the print job: ^FN1^FDvalue^FS → {1: "value"}
			Dictionary<int, string> fieldValues = FdPattern.Matches(zpl)
				.ToDictionary(
					m => int.Parse(m.Groups["num"].Value),
					m => m.Groups["value"].Value);

			this.Logger.LogDebug("Recalling format '{device}:{filename}' with {count} field value(s).", device, filename, fieldValues.Count);

			// Substitute ^FN placeholders in the template body with their ^FD values.
			string populatedBody = FnPlaceholderPattern.Replace(templateBody, match =>
			{
				int num = int.Parse(match.Groups["num"].Value);

				if (fieldValues.TryGetValue(num, out string value))
				{
					return $"^FD{value}^FS";
				}

				// No value supplied for this field — emit empty ^FD so the label
				// renders with a blank field rather than a raw ^FN placeholder.
				this.Logger.LogWarning("No ^FD value supplied for ^FN{num} in format '{device}:{filename}'.", num, device, filename);
				return "^FD^FS";
			});

			// Build the replacement ZPL: keep the outer ^XA/^XZ from the print job
			// and replace the ^XF line (plus the loose ^FN^FD lines) with the body.
			// Remove all ^FN^FD lines from the print job since they've been merged.
			string withoutFd  = FdPattern.Replace(zpl, string.Empty);
			string withBody   = withoutFd.Replace(xfMatch.Value, populatedBody);

			return withBody;
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
				SlidingExpiration              = TimeSpan.FromMinutes(15)
			});

			return body;
		}
	}
}
