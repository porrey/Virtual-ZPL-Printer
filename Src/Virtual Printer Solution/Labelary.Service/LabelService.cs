using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Labelary.Abstractions;

namespace Labelary.Service
{
	public class LabelService : ILabelService
	{
		private static string BaseUrl = "http://api.labelary.com/v1/printers";

		public async Task<byte[]> GetLabelAsync(ILabelConfiguration labelConfiguration, string zpl)
		{
			byte[] returnValue = Array.Empty<byte>();

			using (HttpClient client = new())
			{
				using (StringContent content = new(zpl, Encoding.UTF8, "application/x-www-form-urlencoded"))
				{
					using (HttpResponseMessage response = await client.PostAsync($"{BaseUrl}/{labelConfiguration.Dpmm}dpmm/labels/{labelConfiguration.LabelWidth}x{labelConfiguration.LabelHeight}/0/", content))
					{
						if (response.IsSuccessStatusCode)
						{
							returnValue = await response.Content.ReadAsByteArrayAsync();
						}
						else
						{
							returnValue = this.CreateErrorImage(labelConfiguration, response.ReasonPhrase);
						}
					}
				}
			}

			return returnValue;
		}

		protected byte[] CreateErrorImage(ILabelConfiguration labelConfiguration, string error)
		{
			byte[] returnValue = null;

			//
			// Get the size in pixels
			//
			int width = (int)(labelConfiguration.LabelWidth * 25.4 * labelConfiguration.Dpmm);
			int height = (int)(labelConfiguration.LabelHeight * 25.4 * labelConfiguration.Dpmm);

			//
			// Create an image.
			//
			Bitmap bmp = null;

			using (bmp = new Bitmap(width, height))
			{
				Rectangle rect = new Rectangle(0, 0, width, height);

				using (Graphics graphics = Graphics.FromImage(bmp))
				{
					graphics.FillRectangle(SystemBrushes.Window, rect);

					using (Font font = new("Arial", 12, FontStyle.Bold))
					{
						graphics.DrawString("Labelary Service Error", font, Brushes.DarkBlue, 2, 2);
					}

					using (Font font = new("Arial", 11))
					{
						graphics.DrawString(error, font, Brushes.Black, 2, 55);
					}

					graphics.Flush();
				}

				using (var stream = new MemoryStream())
				{
					bmp.Save(stream, ImageFormat.Png);
					returnValue = stream.ToArray();
				}
			}

			return returnValue;
		}
	}
}
