using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ImageCache.Abstractions;
using Labelary.Abstractions;
using Prism.Events;
using VirtualZplPrinter.Events;
using VirtualZplPrinter.Models;

namespace VirtualZplPrinter.Client
{
	public class TcpListenerClientHandler
	{
		public TcpListenerClientHandler(IEventAggregator eventAggregator, ILabelService labelService, IImageCacheRepository imageCacheRepository)
		{
			this.EventAggregator = eventAggregator;
			this.LabelService = labelService;
			this.ImageCacheRepository = imageCacheRepository;
		}

		protected IEventAggregator EventAggregator { get; set; }
		protected ILabelService LabelService { get; set; }
		protected IImageCacheRepository ImageCacheRepository { get; set; }

		public async Task StartSessionAsync(TcpClient client, ILabelConfiguration labelConfiguration, string imagePathRoot)
		{
			//
			// Get the network stream.
			//
			NetworkStream stream = client.GetStream();

			while (client.Connected && stream.CanRead)
			{
				if (stream.DataAvailable)
				{
					try
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
							//
							// Get the label image
							//
							string zpl = Encoding.UTF8.GetString(buffer);
							byte[] image = await this.LabelService.GetLabelAsync(labelConfiguration, zpl);

							//
							// Save the image.
							//
							IStoredImage storedImage = await this.ImageCacheRepository.StoreImageAsync(imagePathRoot, image);

							//
							// Publish the new label.
							//
							this.EventAggregator.GetEvent<LabelCreatedEvent>().Publish(new LabelCreatedEventArgs()
							{
								PrintRequest = new PrintRequestEventArgs()
								{
									LabelConfiguration = labelConfiguration,
									Zpl = zpl
								},
								Label = storedImage
							});
						}
					}
					catch
					{

					}
					finally
					{
						client.Close();
					}
				}
			}
		}
	}
}
