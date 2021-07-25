using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Labelary.Abstractions;

namespace Labelary.Service
{
	public class LabelService : ILabelService
	{
		public async Task<byte[]> GetLabelAsync(ILabelConfiguration labelConfiguration, string zpl)
		{
			byte[] returnValue = Array.Empty<byte>();

			using (HttpClient client = new())
			{
				using (StringContent content = new(zpl, Encoding.UTF8, "application/x-www-form-urlencoded"))
				{
					using (HttpResponseMessage response = await client.PostAsync($"http://api.labelary.com/v1/printers/{labelConfiguration.Dpmm}dpmm/labels/{labelConfiguration.LabelWidth}x{labelConfiguration.LabelHeight}/0/", content))
					{
						if (response.IsSuccessStatusCode)
						{
							returnValue = await response.Content.ReadAsByteArrayAsync();
						}
						else
						{
							throw new Exception(response.ReasonPhrase);
						}
					}
				}
			}

			return returnValue;
		}
	}
}
