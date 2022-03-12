namespace Labelary.Abstractions
{
	public interface IGetLabelResponse
	{
		bool Result { get; }
		byte[] Label { get; }
		string Error { get; }
	}
}
