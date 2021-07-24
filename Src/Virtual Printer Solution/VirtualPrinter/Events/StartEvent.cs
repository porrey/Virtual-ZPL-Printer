using Prism.Events;
using VirtualPrinter.Models;

namespace VirtualPrinter.Events
{
	public class StartEvent : PubSubEvent<StartEventArgs>
	{
	}

	public class StartEventArgs
	{
		public LabelConfiguration LabelConfiguration { get; set; }
		public int Port { get; set; }
		public string ImagePath { get; set; }
	}
}
