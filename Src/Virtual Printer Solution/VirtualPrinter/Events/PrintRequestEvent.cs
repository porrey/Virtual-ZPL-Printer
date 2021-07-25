using Labelary.Abstractions;
using Prism.Events;

namespace VirtualPrinter.Events
{
	public class PrintRequestEvent : PubSubEvent<PrintRequestEventArgs>
	{
	}

	public class PrintRequestEventArgs
	{
		public ILabelConfiguration LabelConfiguration { get; set; }
		public string Zpl { get; set; }
	}
}
