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
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Labelary.Example
{
	class Program
	{
		static async Task Main(string[] args)
		{
			//
			// Set to false for PNG.
			//
			bool pdf = true;

			//
			// This ZPL string will produce a simple label.
			//
			string zpl = "^xa^cfa,50^fo100,100^fdHello World^fs^xz";

			using (HttpClient client = new())
			{
				Console.WriteLine($"Requesting {(pdf ? "PDF" : "PNG")} format.");
				client.DefaultRequestHeaders.Add("Accept", pdf ? "application/pdf" : "image/png");

				using (StringContent content = new(zpl, Encoding.UTF8, "application/x-WWW-form-urlencoded"))
				{
					Console.WriteLine("Requesting 4x6 label...");
					
					using (HttpResponseMessage response = await client.PostAsync("http://api.labelary.com/v1/printers/8dpmm/labels/4x6/0/", content))
					{
						if (response.IsSuccessStatusCode)
						{
							byte[] labelData = await response.Content.ReadAsByteArrayAsync();
							File.WriteAllBytes($@"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\zpl-label-image.{(pdf ? "pdf" : "png")}", labelData);
							Console.WriteLine("Label saved to desktop.");
						}
						else
						{
							Console.WriteLine($"Error: {response.ReasonPhrase}.");
						}
					}
				}
			}
		}
	}
}
