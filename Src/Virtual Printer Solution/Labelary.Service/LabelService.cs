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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Labelary.Abstractions;
using UnitsNet;

namespace Labelary.Service
{
	public class LabelService : ILabelService
	{
		private static readonly string BaseUrl = "http://api.labelary.com/v1/printers";

		public async Task<IGetLabelResponse> GetLabelAsync(ILabelConfiguration labelConfiguration, string zpl, int labelIndex = 0)
		{
			GetLabelResponse returnValue = new()
			{
				LabelIndex = labelIndex
			};

			try
			{
				using (HttpClient client = new())
				{
					using (StringContent content = new(zpl, Encoding.UTF8, "application/x-www-form-urlencoded"))
					{
						double width = (new Length(labelConfiguration.LabelWidth, labelConfiguration.Unit)).ToUnit(UnitsNet.Units.LengthUnit.Inch).Value;
						double height = (new Length(labelConfiguration.LabelHeight, labelConfiguration.Unit)).ToUnit(UnitsNet.Units.LengthUnit.Inch).Value;

						content.Headers.TryAddWithoutValidation("X-Rotation", Convert.ToString(labelConfiguration.LabelRotation));

						if (width <= 15 && height <= 15)
						{
							string url = $"{BaseUrl}/{labelConfiguration.Dpmm}dpmm/labels/{width:#.##}x{height:#.##}/{labelIndex}/";
							using (HttpResponseMessage response = await client.PostAsync(url, content))
							{
								if (response.IsSuccessStatusCode)
								{
									//
									// Get the label count.
									//
									if (response.Headers.Contains("X-Total-Count"))
									{
										returnValue.LabelCount = Convert.ToInt32(response.Headers.GetValues("X-Total-Count").FirstOrDefault());
									}
									else
									{
										returnValue.LabelCount = 1;
									}

									returnValue.Result = true;
									returnValue.Label = await response.Content.ReadAsByteArrayAsync();
									returnValue.Error = null;
								}
								else
								{
									string error = await response.Content.ReadAsStringAsync();

									returnValue.Result = false;
									returnValue.Label = this.CreateErrorImage(labelConfiguration, "Labelary Error", error ?? response.ReasonPhrase);
									returnValue.Error = error ?? response.ReasonPhrase;
								}
							}
						}
						else
						{
							string message = "Height and Width must be less than or equal to 15 inches.";
							returnValue.Result = false;
							returnValue.Label = this.CreateErrorImage(labelConfiguration, "Invalid Size", message);
							returnValue.Error = message;
						}
					}
				}
			}
			catch (Exception ex)
			{
				returnValue.Result = false;
				returnValue.Label = this.CreateErrorImage(labelConfiguration, "Exception", ex.Message);
				returnValue.Error = ex.Message;
			}

			return returnValue;
		}

		public async Task<IEnumerable<IGetLabelResponse>> GetLabelsAsync(ILabelConfiguration labelConfiguration, string zpl)
		{
			IList<IGetLabelResponse> returnValue = new List<IGetLabelResponse>();

			//
			// Get the first label.
			//
			IGetLabelResponse result = await this.GetLabelAsync(labelConfiguration, zpl, 0);
			returnValue.Add(result);

			if (result.LabelCount > 1)
			{
				//
				// Get the remaining labels.
				//
				for (int labelIndex = 1; labelIndex < result.LabelCount; labelIndex++)
				{
					result = await this.GetLabelAsync(labelConfiguration, zpl, labelIndex);
					returnValue.Add(result);
				}
			}

			return returnValue;
		}

		protected byte[] CreateErrorImage(ILabelConfiguration labelConfiguration, string title, string error)
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

			const int TOP_HEIGHT = 145;
			const int MARGIN = 10;
			const int BORDER = 20;
			const int IMAGE = 128;

			//
			// Create an image.
			//
			using (Bitmap bmp = new(width, height))
			{
				Rectangle rect = new(0, 0, width, height);

				using (Graphics graphics = Graphics.FromImage(bmp))
				{
					Pen linePen = new Pen(new SolidBrush(Color.FromArgb(58, 97, 132)));
					graphics.FillRectangle(SystemBrushes.Window, rect);

					//
					// Draw the label border.
					//
					graphics.DrawRectangle(linePen, BORDER, BORDER, width - (2 * BORDER), height - (2 * BORDER));

					//
					// Draw the image
					//
					Rectangle imageRect = new(BORDER, BORDER, IMAGE, IMAGE);
					graphics.DrawImage(Image.FromFile("./Assets/label.png"), imageRect);

					//
					// Draw the title.
					//
					Rectangle titleLayout = new()
					{
						X = BORDER + IMAGE + MARGIN,
						Y = BORDER,
						Width = width - IMAGE - (2 * BORDER) - MARGIN,
						Height = TOP_HEIGHT
					};

					using (Font font = new("Arial", 16, FontStyle.Bold))
					{
						StringFormat stringFormat = new()
						{
							Alignment = StringAlignment.Near,
							LineAlignment = StringAlignment.Center
						};

						graphics.DrawString(title, font, new SolidBrush(Color.FromArgb(58, 97, 132)), titleLayout, stringFormat);
					}

					//
					// Draw the dividing line.
					//
					graphics.DrawLine(linePen, BORDER, titleLayout.Bottom, width - (1 * BORDER), titleLayout.Bottom);

					//
					// Draw the error text.
					//
					Rectangle bodyLayout = new()
					{
						X = BORDER + MARGIN,
						Y = titleLayout.Bottom + MARGIN,
						Width = width - (2 * BORDER) - (2 * MARGIN),
						Height = height - titleLayout.Bottom - BORDER - (2 * MARGIN)
					};

					using (Font font = new("Arial", 14))
					{
						StringFormat stringFormat = new()
						{
							Alignment = StringAlignment.Near,
						};

						graphics.DrawString(error, font, new SolidBrush(Color.FromArgb(75, 75, 75)), bodyLayout, stringFormat);
					}

					graphics.Flush();
				}

				using (MemoryStream stream = new MemoryStream())
				{
					bmp.Save(stream, ImageFormat.Png);
					returnValue = stream.ToArray();
				}
			}

			return returnValue;
		}
	}
}