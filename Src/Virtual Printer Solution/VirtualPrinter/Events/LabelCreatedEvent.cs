using ImageCache.Abstractions;
using Prism.Events;

namespace VirtualPrinter.Events
{
	public class LabelCreatedEvent : PubSubEvent<LabelCreatedEventArgs>
	{
	}

	public class LabelCreatedEventArgs
	{
		public PrintRequestEventArgs PrintRequest { get; set; }
		public IStoredImage Label { get; set; }
	}
}