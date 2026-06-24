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

namespace VirtualPrinter.GrfStorageService
{
	internal class GrfStorageService(ILogger<GrfStorageService> logger, IMemoryCache memoryCache) : IGrfStorageService
	{
		protected ILogger<GrfStorageService> Logger { get; } = logger;
		protected IMemoryCache MemoryCache { get; } = memoryCache;

		// Matches the full ~DG blob, including multi-line hex data.
		// GRF data never contains '^', so [^\^]+ captures all continuation
		// lines and stops naturally at the next ZPL command (e.g. ^XZ).
		private static readonly Regex DgPattern = new(
			@"~DG(?<device>[A-Z]):(?<filename>[\w]+\.GRF),[^\^]+",
			RegexOptions.Compiled);

		// Matches:  ^XGE:NFRC.GRF  or  ^XGE:10ALL.GRF  etc.
		private static readonly Regex XgPattern = new(
			@"\^XG(?<device>[A-Z]):(?<filename>[\w]+\.GRF)",
			RegexOptions.Compiled);

		public DirectoryInfo GrfDirectory => new(
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
						 "Virtual ZPL Printer", "Graphics"));

		public async Task SaveGrfFromZplAsync(string zpl)
		{
			MatchCollection matches = DgPattern.Matches(zpl);

			if (matches.Count == 0)
			{
				return;
			}

			this.GrfDirectory.Create();

			foreach (Match match in matches)
			{
				string device   = match.Groups["device"].Value;
				string filename = match.Groups["filename"].Value;
				string key      = $"{device}_{filename}";
				string path     = Path.Combine(this.GrfDirectory.FullName, key);

				//
				// Persist the full ~DG line verbatim so it can be injected
				// directly into a later ZPL payload without transformation.
				//
				await File.WriteAllTextAsync(path, match.Value);
				this.MemoryCache.Remove(key);

				this.Logger.LogInformation("Saved GRF '{device}:{filename}' to flash library.", device, filename);
			}
		}

		public async Task<string> ApplyReferencedGrfAsync(string zpl)
		{
			if (string.IsNullOrWhiteSpace(zpl) || !zpl.Contains("^XG"))
			{
				return zpl;
			}

			MatchCollection matches = XgPattern.Matches(zpl);

			if (matches.Count == 0)
			{
				return zpl;
			}

			//
			// Collect unique device:filename pairs referenced in this label.
			//
			var refs = matches
				.Select(m => (Device: m.Groups["device"].Value, Filename: m.Groups["filename"].Value))
				.Distinct()
				.ToArray();

			StringBuilder injected = new();

			foreach ((string device, string filename) in refs)
			{
				string blob = await this.GetGrfBlobAsync(device, filename);

				if (blob != null)
				{
					injected.AppendLine(blob);
					this.Logger.LogDebug("Injected GRF '{device}:{filename}' into ZPL payload.", device, filename);
				}
				else
				{
					this.Logger.LogWarning("GRF '{device}:{filename}' referenced in ZPL but not found in flash library. " +
										   "Send the corresponding *ImageTemplate.zpl to the virtual printer first.", device, filename);
				}
			}

			if (injected.Length == 0)
			{
				return zpl;
			}

			//
			// Prepend the ~DG blobs immediately after ^XA, mirroring the
			// approach used by FontService for ~DU font injection.
			//
			string trimmed = zpl.TrimStart();

			if (trimmed.StartsWith("^XA"))
			{
				return zpl.Replace("^XA", $"^XA\r\n{injected}");
			}

			return injected + zpl;
		}

		private async Task<string> GetGrfBlobAsync(string device, string filename)
		{
			string key = $"{device}_{filename}";

			if (this.MemoryCache.TryGetValue(key, out string cached))
			{
				return cached;
			}

			string path = Path.Combine(this.GrfDirectory.FullName, key);

			if (!File.Exists(path))
			{
				return null;
			}

			string blob = await File.ReadAllTextAsync(path);

			this.MemoryCache.Set(key, blob, new MemoryCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
				SlidingExpiration              = TimeSpan.FromMinutes(15)
			});

			return blob;
		}
	}
}
