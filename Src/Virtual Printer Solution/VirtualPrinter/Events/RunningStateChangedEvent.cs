using Prism.Events;

namespace VirtualPrinter.Events
{
	public class RunningStateChangedEvent : PubSubEvent<RunningStateChangedEventArgs>
	{
	}

	public class RunningStateChangedEventArgs
	{
		public bool IsRunning { get; set; }
		public bool IsError { get; set; }
		public string ErrorMessage { get; set; }
	}
}
