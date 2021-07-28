using UnitsNet.Units;

namespace VirtualZplPrinter.Models
{
	public class LabelUnit
	{
		public LengthUnit Unit { get; set; }
		public string Display => this.Unit.ToString();
	}
}
