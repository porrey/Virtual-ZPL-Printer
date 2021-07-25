using System;

namespace ImageCache.Abstractions
{
	public interface IStoredImage
	{
		int Id { get; set; }
		string FullPath { get; set; }
		public DateTime Timestamp { get; set; }
	}
}
