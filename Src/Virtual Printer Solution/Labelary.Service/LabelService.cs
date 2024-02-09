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
using System.Globalization;
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

			this.Logger.LogInformation("Calling Labelary to get label index {index}.", labelIndex);

			if (this.LabelServiceConfiguration.Method == "POST")
			{
				this.Logger.LogDebug("Calling Labelary using POST method.");
				returnValue = this.GetLabelViaPostAsync(labelConfiguration, zpl, labelIndex);
			}
			else
			{
				this.Logger.LogDebug("Calling Labelary using GET method.");
				returnValue = this.GetLabelViaGetAsync(labelConfiguration, zpl, labelIndex);
			}

			return returnValue;
		}

		public async Task<IEnumerable<IGetLabelResponse>> GetLabelsAsync(ILabelConfiguration labelConfiguration, string zpl)
		{
			IList<IGetLabelResponse> returnValue = [];

			//
			// Get the first label.
			//
			this.Logger.LogDebug("Calling Labelary to first label.");
			IGetLabelResponse result = await this.GetLabelAsync(labelConfiguration, zpl, 0);
			returnValue.Add(result);

			if (result.LabelCount > 1)
			{
				this.Logger.LogDebug("Calling Labelary to get additional labels.");

				//
				// Get the remaining labels.
				//
				for (int labelIndex = 1; labelIndex < result.LabelCount; labelIndex++)
				{
					result = await this.GetLabelAsync(labelConfiguration, zpl, labelIndex);
					returnValue.Add(result);
				}
			}
			else
			{
				this.Logger.LogDebug("No additional labels available.");
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
					//
					// Apply the filters to the ZPL.
					//
					returnValue.Zpl = zpl.Filter(labelConfiguration.LabelFilters);
					this.Logger.LogDebug("Filtered ZPL: '{zpl}'.", returnValue.Zpl.Replace("\"", ""));

					using (StringContent content = new(returnValue.Zpl, Encoding.UTF8, "application/x-www-form-urlencoded"))
					{
						double width = new Length(labelConfiguration.LabelWidth, labelConfiguration.Unit).ToUnit(UnitsNet.Units.LengthUnit.Inch).Value;
						double height = new Length(labelConfiguration.LabelHeight, labelConfiguration.Unit).ToUnit(UnitsNet.Units.LengthUnit.Inch).Value;

						//
						// Add the rotation header,
						//
						this.Logger.LogDebug("Adding request header X-Rotation with value of '{value}'.", labelConfiguration.LabelRotation);
						content.Headers.TryAddWithoutValidation("X-Rotation", Convert.ToString(labelConfiguration.LabelRotation));

						//
						// Add the Linter header.
						//
						this.Logger.LogDebug("Adding request header X-Linter with value of '{value}'.", this.LabelServiceConfiguration.Linting ? "ON" : "OFF");
						content.Headers.TryAddWithoutValidation("X-Linter", this.LabelServiceConfiguration.Linting ? "ON" : "OFF");

						//
						// Add the header to request a PNG. This is the default but adding in case that changes
						// in a future release.
						//
						content.Headers.TryAddWithoutValidation("Accept", "image/png");

						if (width <= 15 && height <= 15)
						{
							//
							// Force US formatting for Labelary REST API.
							//
							string widthString = width.ToString("#.##", new CultureInfo("us-EN"));
							this.Logger.LogDebug("The Width parameter is '{value}'.", widthString);
							string heightString = height.ToString("#.##", new CultureInfo("us-EN"));
							this.Logger.LogDebug("The Height parameter is '{value}'.", heightString);

							//
							// Build the URL.
							//
							string url = $"{this.LabelServiceConfiguration.BaseUrl}/{labelConfiguration.Dpmm}dpmm/labels/{widthString}x{heightString}/{labelIndex}/";
							this.Logger.LogDebug("The URL for the Labelary REST API is '{url}'.", url);

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
										this.Logger.LogDebug("Labelary reported {count} labels.", returnValue.LabelCount);
									}
									else
									{
										returnValue.LabelCount = 1;
										this.Logger.LogDebug("Labelary reported {count} labels.", returnValue.LabelCount);
									}

									//
									// Get warnings
									//
									if (response.Headers.Contains("X-Warnings"))
									{
										returnValue.Warnings = this.ParseWarnings(response.Headers.GetValues("X-Warnings").FirstOrDefault());
										this.Logger.LogDebug("Labelary warnings: '{warnings}'.", returnValue.Warnings);
									}

									returnValue.Result = true;
									returnValue.Label = await response.Content.ReadAsByteArrayAsync();
									returnValue.Error = null;
								}
								else
								{
									string error = await response.Content.ReadAsStringAsync();
									this.Logger.LogDebug("The response was '{response}'.", error);

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
							this.Logger.LogInformation("The label request height or width is larger than 15 inches.");
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
				this.Logger.LogError(ex, "Exception calling Labelary API.");

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
					//
					// Apply the filters to the ZPL.
					//
					returnValue.Zpl = zpl.Filter(labelConfiguration.LabelFilters);
					this.Logger.LogDebug("Filtered ZPL: '{zpl}'.", returnValue.Zpl);

					//
					// Encode the ZPL for the URL.
					//
					UrlEncoder encoder = UrlEncoder.Create();
					string encodedZpl = encoder.Encode(returnValue.Zpl);
					this.Logger.LogDebug("URL Encoded ZPL: '{zpl}'.", encodedZpl);

					double width = new Length(labelConfiguration.LabelWidth, labelConfiguration.Unit).ToUnit(UnitsNet.Units.LengthUnit.Inch).Value;
					double height = new Length(labelConfiguration.LabelHeight, labelConfiguration.Unit).ToUnit(UnitsNet.Units.LengthUnit.Inch).Value;

					//
					// Add the rotation header,
					//
					this.Logger.LogDebug("Adding request header X-Rotation with value of '{value}'.", labelConfiguration.LabelRotation);
					client.DefaultRequestHeaders.TryAddWithoutValidation("X-Rotation", Convert.ToString(labelConfiguration.LabelRotation));

					//
					// Add the Linter header.
					//
					this.Logger.LogDebug("Adding request header X-Linter with value of '{value}'.", this.LabelServiceConfiguration.Linting ? "ON" : "OFF");
					client.DefaultRequestHeaders.TryAddWithoutValidation("X-Linter", this.LabelServiceConfiguration.Linting ? "ON" : "OFF");

					//
					// Add the header to request a PNG. This is the default but adding in case that changes
					// in a future release.
					//
					client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "image/png");

					if (width <= 15 && height <= 15)
					{
						//
						// Force US formatting for Labelary REST API.
						//
						string widthString = width.ToString("#.##", new CultureInfo("us-EN"));
						this.Logger.LogDebug("The Width parameter is '{value}'.", widthString);
						string heightString = height.ToString("#.##", new CultureInfo("us-EN"));
						this.Logger.LogDebug("The Height parameter is '{value}'.", heightString);

						//
						// Build the URL.
						//
						string url = $"{this.LabelServiceConfiguration.BaseUrl}/{labelConfiguration.Dpmm}dpmm/labels/{widthString}x{heightString}/{labelIndex}/{encodedZpl}";
						this.Logger.LogDebug("The URL for the Labelary REST API is '{url}'.", url);

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
									this.Logger.LogDebug("Labelary reported {count} labels.", returnValue.LabelCount);
								}
								else
								{
									returnValue.LabelCount = 1;
									this.Logger.LogDebug("Labelary reported {count} labels.", returnValue.LabelCount);
								}

								//
								// Get warnings
								//
								if (response.Headers.Contains("X-Warnings"))
								{
									returnValue.Warnings = this.ParseWarnings(response.Headers.GetValues("X-Warnings").FirstOrDefault());
									this.Logger.LogDebug("Labelary warnings: '{warnings}'.", returnValue.Warnings);
								}

								returnValue.Result = true;
								returnValue.Label = await response.Content.ReadAsByteArrayAsync();
								returnValue.Error = null;
							}
							else
							{
								string error = await response.Content.ReadAsStringAsync();
								this.Logger.LogDebug("The response was '{response}'.", error);

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
						this.Logger.LogInformation("The label request height or width is larger than 15 inches.");
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
				this.Logger.LogError(ex, "Exception calling Labelary API.");

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
						ParameterNumber = !string.IsNullOrWhiteSpace(parts[i + 3]) ? Convert.ToInt32(parts[i + 3]) : 0,
						Message = parts[i + 4]
					};

					returnValue.Add(warning);
				}
			}

			return returnValue;
		}
	}
}