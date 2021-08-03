using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageCache.Abstractions;

namespace ImageCache.Repository
{
	public class ImageCacheRepository : IImageCacheRepository
	{
		public string DefaultFolder => $@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\Virtual ZPL Printer Images";
		protected string FileName(DirectoryInfo imagePathRoot, int id) => $@"{imagePathRoot.FullName}\zpl-label-image-{id}.png";
		protected FileInfo[] GetFiles(DirectoryInfo imagePathRoot) => imagePathRoot.GetFiles("zpl-label-image-*.png").OrderBy(t => t.CreationTime).ToArray();
		protected int GetFileIndex(FileInfo file) => Convert.ToInt32(Path.GetFileNameWithoutExtension(file.Name).Split(new char[] { '-' })[3]);
		protected int[] GetFileIndices(DirectoryInfo imagePathRoot) => this.GetFiles(imagePathRoot).Select(t => this.GetFileIndex(t)).ToArray();

		protected DirectoryInfo GetDirectory(string imagePathRoot)
		{
			DirectoryInfo returnValue = null;

			if (string.IsNullOrWhiteSpace(imagePathRoot))
			{
				imagePathRoot = this.DefaultFolder;
			}

			returnValue = new(imagePathRoot);

			if (!returnValue.Exists)
			{
				returnValue.Create();
			}

			return returnValue;
		}

		protected int GetNextIndex(DirectoryInfo imagePathRoot)
		{
			int returnValue = 1;

			int[] indices = this.GetFileIndices(imagePathRoot);

			if (indices.Any())
			{
				returnValue = indices.Max() + 1;
			}

			return returnValue;
		}

		public async Task<IStoredImage> StoreImageAsync(string imagePathRoot, byte[] pngImage)
		{
			IStoredImage returnValue = new StoredImage();

			//
			// Ensure the path exists.
			//
			DirectoryInfo dir = this.GetDirectory(imagePathRoot);

			returnValue.Id = this.GetNextIndex(dir);
			returnValue.FullPath = this.FileName(dir, returnValue.Id);

			await File.WriteAllBytesAsync(returnValue.FullPath, pngImage);

			return returnValue;
		}

		public Task<IEnumerable<IStoredImage>> GetAllAsync(string imagePathRoot)
		{
			IList<IStoredImage> returnValue = new List<IStoredImage>();

			DirectoryInfo dir = this.GetDirectory(imagePathRoot);

			if (dir.Exists)
			{
				FileInfo[] files = this.GetFiles(dir);

				foreach (FileInfo file in files)
				{
					returnValue.Add(new StoredImage()
					{
						Id = this.GetFileIndex(file),
						FullPath = file.FullName,
						Timestamp = file.CreationTime
					});
				}
			}

			return Task.FromResult<IEnumerable<IStoredImage>>(returnValue.OrderBy(t => t.Timestamp));
		}

		public Task<bool> ClearAllAsync(string imagePathRoot)
		{
			bool returnValue = false;

			DirectoryInfo dir = this.GetDirectory(imagePathRoot);
			int errorCount = 0;

			if (dir.Exists)
			{
				FileInfo[] files = this.GetFiles(dir);

				foreach (FileInfo file in files)
				{
					try
					{
						file.Delete();
					}
					catch
					{
						errorCount++;
					}
				}

				returnValue = (errorCount == 0);
			}

			return Task.FromResult(returnValue);
		}

		public Task<bool> DeleteImageAsync(string imagePathRoot, string imageName)
		{
			bool returnValue = false;

			DirectoryInfo dir = this.GetDirectory(imagePathRoot);

			if (dir.Exists)
			{
				FileInfo file = dir.GetFiles().Where(t => t.Name == imageName).SingleOrDefault();

				if (file != null)
				{
					file.Delete();
					returnValue = true;
				}
			}

			return Task.FromResult(returnValue);
		}
	}
}
