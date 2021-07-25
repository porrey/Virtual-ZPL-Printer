using Labelary.Abstractions;

namespace VirtualPrinter.Models
{
	public class LabelConfiguration : ILabelConfiguration
	{
		public int Dpmm { get; set; }
		public double LabelWidth { get; set; }
		public double LabelHeight { get; set; }
	}
}
