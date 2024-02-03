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
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Labelary.Abstractions;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace Labelary.Service
{
	public class LabelService : ILabelService
	{
		public LabelService(ILogger<LabelService> logger, ILabelServiceConfiguration labelServiceConfiguration)
		{
			this.Logger = logger;
			this.LabelServiceConfiguration = labelServiceConfiguration;
		}

		protected ILogger<LabelService> Logger { get; set; }
		public ILabelServiceConfiguration LabelServiceConfiguration { get; set; }

		public Task<IGetLabelResponse> GetLabelAsync(ILabelConfiguration labelConfiguration, string zpl, int labelIndex = 0)
		{
			Task<IGetLabelResponse> returnValue = null;

			if (this.LabelServiceConfiguration.Method == "POST")
			{
				returnValue = this.GetLabelViaPostAsync(labelConfiguration, zpl, labelIndex);
			}
			else
			{
				returnValue = this.GetLabelViaGetAsync(labelConfiguration, zpl, labelIndex);
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

		protected async Task<IGetLabelResponse> GetLabelViaPostAsync(ILabelConfiguration labelConfiguration, string zpl, int labelIndex = 0)
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
					returnValue.Zpl = zpl.Filter(labelConfiguration.LabelFilters);

					using (StringContent content = new(returnValue.Zpl, Encoding.UTF8, "application/x-www-form-urlencoded"))
					{
						double width = new Length(labelConfiguration.LabelWidth, labelConfiguration.Unit).ToUnit(UnitsNet.Units.LengthUnit.Inch).Value;
						double height = new Length(labelConfiguration.LabelHeight, labelConfiguration.Unit).ToUnit(UnitsNet.Units.LengthUnit.Inch).Value;

						//
						// Add the rotation header,
						//
						content.Headers.TryAddWithoutValidation("X-Rotation", Convert.ToString(labelConfiguration.LabelRotation));

						//
						// Add the Linter header.
						//
						content.Headers.TryAddWithoutValidation("X-Linter", this.LabelServiceConfiguration.Linting ? "ON" : "OFF");

						if (width <= 15 && height <= 15)
						{
							string url = $"{this.LabelServiceConfiguration.BaseUrl}/{labelConfiguration.Dpmm}dpmm/labels/{width:#.##}x{height:#.##}/{labelIndex}/";

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

									//
									// Get warnings
									//
									if (response.Headers.Contains("X-Warnings"))
									{
										returnValue.Warnings = this.ParseWarnings(response.Headers.GetValues("X-Warnings").FirstOrDefault());
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

		protected async Task<IGetLabelResponse> GetLabelViaGetAsync(ILabelConfiguration labelConfiguration, string zpl, int labelIndex = 0)
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
					UrlEncoder encoder = UrlEncoder.Create();
					returnValue.Zpl = zpl.Filter(labelConfiguration.LabelFilters);
					string encodedZpl = encoder.Encode(returnValue.Zpl);

					double width = new Length(labelConfiguration.LabelWidth, labelConfiguration.Unit).ToUnit(UnitsNet.Units.LengthUnit.Inch).Value;
					double height = new Length(labelConfiguration.LabelHeight, labelConfiguration.Unit).ToUnit(UnitsNet.Units.LengthUnit.Inch).Value;

					//
					// Add the rotation header,
					//
					client.DefaultRequestHeaders.TryAddWithoutValidation("X-Rotation", Convert.ToString(labelConfiguration.LabelRotation));

					//
					// Add the Linter header.
					//
					client.DefaultRequestHeaders.TryAddWithoutValidation("X-Linter", this.LabelServiceConfiguration.Linting ? "ON" : "OFF");

					if (width <= 15 && height <= 15)
					{
						string url = $"{this.LabelServiceConfiguration.BaseUrl}/{labelConfiguration.Dpmm}dpmm/labels/{width:#.##}x{height:#.##}/{labelIndex}/{encodedZpl}";

						using (HttpResponseMessage response = await client.GetAsync(url))
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

								//
								// Get warnings
								//
								if (response.Headers.Contains("X-Warnings"))
								{
									returnValue.Warnings = this.ParseWarnings(response.Headers.GetValues("X-Warnings").FirstOrDefault());
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

		protected IEnumerable<Warning> ParseWarnings(string warnings)
		{
			IList<Warning> returnValue = [];

			if (!string.IsNullOrWhiteSpace(warnings))
			{
				string[] parts = warnings.Split('|');

				for (int i = 0; i < parts.Length; i += 5)
				{
					Warning warning = new()
					{
						ByteIndex = Convert.ToInt32(parts[i]),
						ByteSize = Convert.ToInt32(parts[i + 1]),
						ZplCommand = parts[i + 2],
						ParameterNumber = Convert.ToInt32(parts[i + 3]),
						Message = parts[i + 4]
					};

					returnValue.Add(warning);
				}
			}

			return returnValue;
		}
	}
}