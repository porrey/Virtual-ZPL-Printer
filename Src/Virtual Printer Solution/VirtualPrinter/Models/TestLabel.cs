using System;
using System.IO;
using System.Threading.Tasks;

namespace VirtualZplPrinter
{
	public static class TestLabel
	{
		private static Random Rnd { get; } = new Random();

		public static Task<string> GetZplAsync()
		{
			string returnValue = null;

			//
			// Create a random bar code value for the label.
			//
			int id = TestLabel.Rnd.Next(1, 99999999);

			//
			// Read the sample ZPL.
			//
			returnValue = File.ReadAllText("./samples/6x4-203dpi.txt").Replace("{id}", id.ToString("00000000"));

			return Task.FromResult(returnValue);
		}
	}
}
