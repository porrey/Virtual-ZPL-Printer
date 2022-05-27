namespace Labelary.Abstractions
{
	public interface IGetLabelResponse
	{
		int LabelIndex { get; set; }
		int LabelCount { get; set; }
		bool Result { get; }
		byte[] Label { get; }
		string Error { get; }
		bool HasMultipleLabels { get; }
	}
}
