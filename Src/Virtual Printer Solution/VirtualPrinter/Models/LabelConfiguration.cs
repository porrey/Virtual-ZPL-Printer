using Labelary.Abstractions;

namespace VirtualZplPrinter.Models
{
	public class LabelConfiguration : ILabelConfiguration
	{
		public int Dpmm { get; set; }
		public double LabelWidth { get; set; }
		public double LabelHeight { get; set; }
	}
}
