using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;
using VirtualPrinter.Events;
using VirtualPrinter.Models;

namespace VirtualPrinter.Client
{
	public class TcpListenerClientHandler
	{
		public TcpListenerClientHandler(IEventAggregator eventAggregator)
		{
			this.EventAggregator = eventAggregator;
		}

		protected IEventAggregator EventAggregator { get; set; }
		private static int id = 0;

		public async Task StartSessionAsync(TcpClient client, LabelConfiguration labelConfiguration, string imagePathRoot)
		{
			//
			// Get the network stream.
			//
			NetworkStream stream = client.GetStream();

			while (client.Connected && stream.CanRead)
			{
				if (stream.DataAvailable)
				{
					//
					// Create a buffer for the data that is available.
					//
					byte[] buffer = new byte[client.Available];

					//
					// Read the data into the buffer.
					//
					int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));

					if (bytesRead > 0)
					{
						string zpl = ASCIIEncoding.UTF8.GetString(buffer);
						string imagePath = await this.GetLabel(imagePathRoot, labelConfiguration, zpl);

						this.EventAggregator.GetEvent<LabelCreatedEvent>().Publish(new LabelCreatedEventArgs()
						{
							PrintRequest = new PrintRequestEventArgs()
							{
								LabelConfiguration = labelConfiguration,
								Zpl = zpl
							},
							Label = new Label()
							{
								ImagePath = imagePath,
								Zpl = zpl
							}
						});
					}

					client.Close();
				}
			}
		}

		protected async Task<string> GetLabel(string imagePathRoot, LabelConfiguration labelConfiguration, string zpl)
		{
			string returnValue = null;

			if (!Directory.Exists(imagePathRoot))
			{
				Directory.CreateDirectory(imagePathRoot);
			}

			using (HttpClient client = new HttpClient())
			{
				using (StringContent content = new StringContent(zpl, Encoding.UTF8, "application/x-www-form-urlencoded"))
				{
					using (HttpResponseMessage response = await client.PostAsync($"http://api.labelary.com/v1/printers/{labelConfiguration.Dpmm}dpmm/labels/{labelConfiguration.LabelWidth}x{labelConfiguration.LabelHeight}/0/", content))
					{
						if (response.IsSuccessStatusCode)
						{
							byte[] image = await response.Content.ReadAsByteArrayAsync();
							returnValue = $@"{imagePathRoot}\zpl-label-image-{id}.png";
							id++;
							File.WriteAllBytes(returnValue, image);
						}
						else
						{
							//throw new Exception(response.ReasonPhrase);
						}
					}
				}
			}

			return returnValue;
		}
	}
}
