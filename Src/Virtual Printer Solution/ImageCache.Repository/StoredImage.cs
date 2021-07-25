using System;
using ImageCache.Abstractions;

namespace ImageCache.Repository
{
	public class StoredImage : IStoredImage
	{
		public int Id { get; set; }
		public string FullPath { get; set; }
		public DateTime Timestamp { get; set; }
	}
}
