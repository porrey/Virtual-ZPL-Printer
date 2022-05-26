using Labelary.Abstractions;

namespace Labelary.Service
{
	internal class GetLabelResponse : IGetLabelResponse
	{
		public int LabelIndex { get; set; }
		public bool Result { get; set; }
		public byte[] Label { get; set; }
		public string Error { get; set; }
		public bool HasMultipleLabels { get; set; }
	}
}
