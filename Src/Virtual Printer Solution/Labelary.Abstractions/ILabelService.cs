using System.Threading.Tasks;

namespace Labelary.Abstractions
{
	public interface ILabelService
	{
		Task<byte[]> GetLabelAsync(ILabelConfiguration labelConfiguration, string zpl);
	}
}
