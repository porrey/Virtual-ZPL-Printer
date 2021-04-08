using Prism.Events;
using VirtualPrinter.Models;

namespace VirtualPrinter.Events
{
	public class PrintRequestEvent : PubSubEvent<PrintRequestEventArgs>
	{
	}

	public class PrintRequestEventArgs
	{
		public LabelConfiguration LabelConfiguration { get; set; }
		public string Zpl { get; set; }
	}
}
