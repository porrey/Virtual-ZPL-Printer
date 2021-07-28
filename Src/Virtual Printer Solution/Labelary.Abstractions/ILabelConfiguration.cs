using UnitsNet.Units;

namespace Labelary.Abstractions
{
	public interface ILabelConfiguration
	{
		int Dpmm { get; set; }
		double LabelHeight { get; set; }
		double LabelWidth { get; set; }
		LengthUnit Unit { get; set; }
	}
}