using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Labelary.Abstractions;
using UnitsNet;

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
					double width = (new Length(labelConfiguration.LabelWidth, labelConfiguration.Unit)).ToUnit(UnitsNet.Units.LengthUnit.Inch).Value;
					double height = (new Length(labelConfiguration.LabelHeight, labelConfiguration.Unit)).ToUnit(UnitsNet.Units.LengthUnit.Inch).Value;
					
					using (HttpResponseMessage response = await client.PostAsync($"{BaseUrl}/{labelConfiguration.Dpmm}dpmm/labels/{width}x{height}/0/", content))
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
			// Get the label size in mm
			//
			double labelWidthMm = (new Length(labelConfiguration.LabelWidth, labelConfiguration.Unit)).ToUnit(UnitsNet.Units.LengthUnit.Millimeter).Value;
			double labelHeightMm = (new Length(labelConfiguration.LabelHeight, labelConfiguration.Unit)).ToUnit(UnitsNet.Units.LengthUnit.Millimeter).Value;

			//
			// Get the size in pixels
			//
			int width = (int)(labelWidthMm * labelConfiguration.Dpmm);
			int height = (int)(labelHeightMm * labelConfiguration.Dpmm);

			//
			// Create an image.
			//
			using (Bitmap bmp = new Bitmap(width, height))
			{
				Rectangle rect = new(0, 0, width, height);

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
