using System.Collections.Generic;
using System.Threading.Tasks;

namespace ImageCache.Abstractions
{
	public interface IImageCacheRepository
	{
		Task<IStoredImage> StoreImageAsync(string imagePathRoot, byte[] pngImage);
		Task<bool> ClearAllAsync(string imagePathRoot);
		Task<IEnumerable<IStoredImage>> GetAllAsync(string imagePathRoot);
	}
}
