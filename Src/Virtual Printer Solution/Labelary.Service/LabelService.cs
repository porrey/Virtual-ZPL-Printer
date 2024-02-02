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
		public string BaseUrl => "http://api.labelary.com/v1/printers";

		public async Task<IGetLabelResponse> GetLabelAsync(ILabelConfiguration labelConfiguration, string zpl, int labelIndex = 0)
		{
			GetLabelResponse returnValue = new()
			{
				LabelIndex = labelIndex,
				ImageFileName = zpl.GetParameterValue("ImageFileName", "zpl-label-image")
			};

			try
			{
				using (HttpClient client = new())
				{
					using (StringContent content = new(zpl.Filter(labelConfiguration.LabelFilters), Encoding.UTF8, "application/x-www-form-urlencoded"))
					{
						double width = new Length(labelConfiguration.LabelWidth, labelConfiguration.Unit).ToUnit(UnitsNet.Units.LengthUnit.Inch).Value;
						double height = new Length(labelConfiguration.LabelHeight, labelConfiguration.Unit).ToUnit(UnitsNet.Units.LengthUnit.Inch).Value;

						content.Headers.TryAddWithoutValidation("X-Rotation", Convert.ToString(labelConfiguration.LabelRotation));

						if (width <= 15 && height <= 15)
						{
							string url = $"{this.BaseUrl}/{labelConfiguration.Dpmm}dpmm/labels/{width:#.##}x{height:#.##}/{labelIndex}/";

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

									//
									// Create the error image.
									//
									ErrorImage errorImage = ErrorImage.Create(labelConfiguration, "Labelary Error", error ?? response.ReasonPhrase);

									returnValue.Result = false;
									returnValue.Label = errorImage.ImageData;
									returnValue.Error = error ?? response.ReasonPhrase;
								}
							}
						}
						else
						{
							//
							// Create the message.
							//
							string message = "Height and Width must be less than or equal to 15 inches.";

							//
							// Create the error image.
							//
							ErrorImage errorImage = ErrorImage.Create(labelConfiguration, "Invalid Size", message);

							returnValue.Result = false;
							returnValue.Label = errorImage.ImageData;
							returnValue.Error = message;
						}
					}
				}
			}
			catch (Exception ex)
			{
				//
				// Create the error image.
				//
				ErrorImage errorImage = ErrorImage.Create(labelConfiguration, "Exception", ex.Message);

				returnValue.Result = false;
				returnValue.Label = errorImage.ImageData;
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
	}
}