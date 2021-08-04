using System.Net;
using Prism.Events;
using VirtualZplPrinter.Models;

namespace VirtualZplPrinter.Events
{
	public class StartEvent : PubSubEvent<StartEventArgs>
	{
	}

	public class StartEventArgs
	{
		public LabelConfiguration LabelConfiguration { get; set; }
		public IPAddress IpAddress { get; set; }
		public int Port { get; set; }
		public string ImagePath { get; set; }
	}
}
