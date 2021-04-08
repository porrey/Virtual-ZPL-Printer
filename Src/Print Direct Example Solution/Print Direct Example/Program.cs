using System.Threading.Tasks;

namespace ConsoleApp1
{
	class Program
	{
		static Task Main(string[] args)
		{
			return PrintDirect.SendUtf8TextAsync(@"\\localhost\4BARCODE", "Hello World");
		}
	}
}
