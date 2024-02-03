namespace Labelary.Abstractions
{
	public class Warning
	{
		public int ByteIndex { get; set; }
		public int ByteSize { get; set; }
		public string ZplCommand { get; set; }
		public int ParameterNumber { get; set; }
		public string Message { get; set; }

		public override string ToString()
		{
			return $"{(!string.IsNullOrWhiteSpace(this.ZplCommand) ? this.ZplCommand : "N/A")} [parameter {this.ParameterNumber}]: {this.Message}";
		}
	}
}
