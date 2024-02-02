namespace VirtualPrinter.Abstractions
{
	public class PhysicalPrinter
	{
		public bool Enabled { get; set; }
		public string PrinterName { get; set; }
		public bool VerticalAlignTop { get; set; } = true;
		public bool VerticalAlignMiddle { get; set; }
		public bool VerticalAlignBottom { get; set; }
		public bool HorizontalAlignLeft { get; set; } = true;
		public bool HorizontalAlignCenter { get; set; }
		public bool HorizontalAlignRight { get; set; }
		public double LeftMargin { get; set; }
		public double RightMargin { get; set; }
		public double TopMargin { get; set; }
		public double BottomMargin { get; set; }
	}
}
