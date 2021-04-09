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
			bool pdf = true;
			string zpl = "^xa^cfa,50^fo100,100^fdHello World^fs^xz";

			using (HttpClient client = new HttpClient())
			{
				if (pdf)
				{
					Console.WriteLine("Requesting PDF format.");
					client.DefaultRequestHeaders.Add("Accept", "application/pdf");
				}

				using (StringContent content = new StringContent(zpl, Encoding.UTF8, "application/x-www-form-urlencoded"))
				{
					Console.WriteLine("Retrieving label...");
					using (HttpResponseMessage response = await client.PostAsync("http://api.labelary.com/v1/printers/8dpmm/labels/4x6/0/", content))
					{
						if (response.IsSuccessStatusCode)
						{
							byte[] image = await response.Content.ReadAsByteArrayAsync();
							string extension = pdf ? "pdf" : "png";
							File.WriteAllBytes($@"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\zpl-label-image.{extension}", image);
							Console.WriteLine("Label image saved to desktop.");
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
