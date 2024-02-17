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
using Newtonsoft.Json;

namespace VirtualPrinter.FontService
{
	internal class FontService(ILogger<FontService> logger, IMemoryCache memoryCache) : IFontService
	{
		protected ILogger<FontService> Logger { get; set; } = logger;
		protected IMemoryCache MemoryCache { get; set; } = memoryCache;

		public DirectoryInfo FontDirectory => new($"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/Virtual ZPL Printer/Fonts");

		public Task<IPrinterFont> CreateAsync()
		{
			return Task.FromResult<IPrinterFont>(new PrinterFont()
			{
				BaseFileName = $"{Guid.NewGuid()}",
				PrinterDevice = "R"
			});
		}

		public Task<IEnumerable<IPrinterFont>> GetFontsAsync()
		{
			IEnumerable<IPrinterFont> returnValue = [];

			//
			// Make sure the directory exists.
			//
			this.FontDirectory.Create();

			//
			// Load the fonts.
			//
			returnValue = from tbl in this.FontDirectory.EnumerateFiles()
						  where tbl.Extension == ".json"
						  select PrinterFont.FromFile(tbl);

			return Task.FromResult(returnValue);
		}

		public async Task<bool> SaveFontAsync(IPrinterFont font, string fontZpl)
		{
			bool returnValue = false;

			string fontDetailsPath = $"{this.FontDirectory.FullName}/{font.BaseFileName}.json";
			string fontPath = $"{this.FontDirectory.FullName}/{font.BaseFileName}.ZPL";
			string json = JsonConvert.SerializeObject(font, Formatting.Indented);
			await File.WriteAllTextAsync(fontDetailsPath, json);
			await File.WriteAllTextAsync(fontPath, fontZpl);

			//
			// Remove this item from the cache.
			//
			if (this.MemoryCache.TryGetValue(font.BaseFileName, out object _))
			{
				this.MemoryCache.Remove(font.BaseFileName);
			}

			return returnValue;
		}

		public async Task<string> GetFontZplAsync(IPrinterFont font)
		{
			string fontPath = $"{this.FontDirectory.FullName}/{font.BaseFileName}.ZPL";
			string returnValue = await File.ReadAllTextAsync(fontPath);
			return returnValue;
		}

		public async Task<IEnumerable<IPrinterFont>> GetReferencedFontsAsync(string zpl)
		{
			IEnumerable<IPrinterFont> returnValue = [];

			//
			// Get the font list.
			//
			IEnumerable<IPrinterFont> fonts = await this.GetFontsAsync();

			//
			// Generate the pattern. 
			//
			string pattern = @"(\^CW)([A-Z0-9])(\,)(?<device>[REBA])(:)(?<font>[\w]{1,16})(\.)(?<extension>[FNT|TTF|TTE]{1,3})";

			//
			// Find the font references in the ZPL.
			//
			var items = (from tbl in Regex.Matches(zpl, pattern, RegexOptions.Multiline | RegexOptions.ExplicitCapture)
						 select new
						 {
							 Device = tbl.Groups["device"].Value,
							 Font = tbl.Groups["font"].Value,
							 Extension = tbl.Groups["extension"].Value
						 }).ToArray();

			//
			// Match the fonts.
			//
			returnValue = (from tbl1 in fonts
						   join tbl2 in items on $"{tbl1.PrinterDevice}:{tbl1.FontName}" equals $"{tbl2.Device}:{tbl2.Font}.{tbl2.Extension}"
						   select tbl1).ToArray();

			return returnValue;
		}

		public async Task<string> ApplyReferencedFontsAsync(IEnumerable<IPrinterFont> fonts, string zpl)
		{
			string returnValue = zpl;

			if (!string.IsNullOrWhiteSpace(zpl) && zpl.Trim().StartsWith("^XA"))
			{
				//
				// Create the string builder.
				//
				StringBuilder insertedZpl = new();

				//
				// Add the start label command.
				//
				insertedZpl.Append("^XA\r\n\r\n");

				//
				// Build the DU commands
				//
				foreach (IPrinterFont font in fonts)
				{
					//
					// Check the cache for the font data.
					//
					string fontData = this.MemoryCache.Get<string>(font.BaseFileName);

					//
					// If not cached, read the data from disk file.
					//
					if (fontData == null)
					{
						//
						// Read the data from the file.
						//
						fontData = await this.GetFontZplAsync(font);

						//
						// Set the cache options.
						//
						MemoryCacheEntryOptions options = new()
						{
							AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
							SlidingExpiration = TimeSpan.FromMinutes(15)
						};

						//
						// Add the data to the cache.
						//
						this.MemoryCache.Set<string>(font.BaseFileName, fontData, options);
					}

					//
					// Add the ~DU command.
					//
					insertedZpl.Append($"~DU{font.PrinterDevice}:{fontData}\r\n\r\n");
				}

				//
				// Update the ZPL and send it back.
				//
				returnValue = zpl.Trim().Replace("^XA", insertedZpl.ToString());
			}

			return returnValue;
		}
	}
}
