using Diamond.Core.Repository;

namespace VirtualPrinter.Db.Abstractions
{
	public interface IPrinterConfiguration : IEntity<int>
	{
		string Name { get; set; }
		string HostAddress { get; set; }
		int Port { get; set; }
		int LabelUnit { get; set; }
		double LabelWidth { get; set; }
		double LabelHeight { get; set; }
		int ResolutionInDpmm { get; set; }
		int RotationAngle { get; set; }
		string ImagePath { get; set; }
		string IdSummary { get; }
		string HostSummary { get; }
		string SizeSummary { get; }
		string ResolutionSummary { get; }
		string RotationSummary { get; }
		string Description { get; }
		string Filters { get; set; }
	}
}