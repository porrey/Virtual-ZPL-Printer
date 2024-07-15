using System.Net;

namespace VirtualPrinter.TestClient
{
	public interface IZplClient
	{
		Task<(bool, string)> SendStringAsync(IPAddress ipAddress, int port, string text, int segmentSize = 1024, int delay = 0);
	}
}
