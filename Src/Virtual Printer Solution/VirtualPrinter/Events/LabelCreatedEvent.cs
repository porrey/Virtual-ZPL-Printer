using Prism.Events;
using VirtualPrinter.Models;

namespace VirtualPrinter.Events
{
	public class LabelCreatedEvent : PubSubEvent<LabelCreatedEventArgs>
	{
	}

	public class LabelCreatedEventArgs
	{
		public PrintRequestEventArgs PrintRequest { get; set; }
		public Label Label { get; set; }
	}
}