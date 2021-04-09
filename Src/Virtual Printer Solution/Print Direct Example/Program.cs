using System.IO;
using System.Threading.Tasks;
using ZplPrinting;

namespace PrintDirectExample
{
	class Program
	{
		static Task Main(string[] args)
		{
			//
			// Load the ZPL file.
			//
			string zpl = File.ReadAllText("./Samples/zpl.txt").Replace("{id}", "00000001");

			//
			// The printer must be shared. My printer is shared as
			// 4BARCODE. Change this to the name of your printer.
			//
			string portName = @"\\localhost\4BARCODE";

			//
			// Send the ZPL text directly to the printer. Defaults
			// to UTF8 encoding.
			//
			return PrintDirect.SendTextAsync(portName, zpl);
		}
	}
}
