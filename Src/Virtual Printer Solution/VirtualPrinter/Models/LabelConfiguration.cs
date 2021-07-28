using Labelary.Abstractions;
using UnitsNet.Units;

namespace VirtualZplPrinter.Models
{
	public class LabelConfiguration : ILabelConfiguration
	{
		public int Dpmm { get; set; }
		public double LabelWidth { get; set; }
		public double LabelHeight { get; set; }
		public LengthUnit Unit { get; set; }
	}
}
