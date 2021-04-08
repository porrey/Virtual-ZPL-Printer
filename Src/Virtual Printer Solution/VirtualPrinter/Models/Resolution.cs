namespace VirtualPrinter.Models
{
	public class Resolution
	{
		public int Dpmm { get; set; }
		public double Dpi => this.Dpmm * 25.4;

		public string Display => $"{this.Dpmm}dpmm [{this.Dpi:0}dpi]";
	}
}
