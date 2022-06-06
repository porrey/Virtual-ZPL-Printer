using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Labelary.Abstractions;
using UnitsNet;

namespace Labelary.Service
{
	public class ErrorImage
	{
		internal ErrorImage()
		{
		}

		public byte[] ImageData { get; protected set; }

		public static ErrorImage Create(ILabelConfiguration labelConfiguration, string title, string error)
		{
			return new()
			{
				ImageData = ErrorImage.CreateImage(labelConfiguration, title, error)
			};
		}

		protected static byte[] CreateImage(ILabelConfiguration labelConfiguration, string title, string error)
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
					Rectangle imageRect = new(BORDER + 2, BORDER + 2, IMAGE, IMAGE);
					graphics.DrawImage(Image.FromFile("./Assets/printer-label.png"), imageRect);

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
