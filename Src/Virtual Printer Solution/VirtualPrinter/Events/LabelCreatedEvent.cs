using ImageCache.Abstractions;
using Prism.Events;

namespace VirtualZplPrinter.Events
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