/*
 *  This file is part of Virtual ZPL Printer.
 *  
 *  Virtual ZPL Printer is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Virtual ZPL Printer is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Virtual ZPL Printer.  If not, see <https://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageCache.Abstractions;
using Labelary.Abstractions;

namespace ImageCache.Repository
{
	public class ImageCacheRepository : IImageCacheRepository
	{
		protected string FileName(DirectoryInfo imagePathRoot, int id) => $@"{imagePathRoot.FullName}\zpl-label-image-{id}.png";
		protected string FileName(DirectoryInfo imagePathRoot, int id, int index) => $@"{imagePathRoot.FullName}\zpl-label-image-{id}-Page{index}.png";
		protected FileInfo[] GetFiles(DirectoryInfo imagePathRoot) => imagePathRoot.GetFiles("zpl-label-image-*.png").OrderBy(t => t.CreationTime).ToArray();
		protected int GetFileIndex(FileInfo file) => Convert.ToInt32(Path.GetFileNameWithoutExtension(file.Name).Split(new char[] { '-' })[3].Replace(" ", ""));
		protected int[] GetFileIndices(DirectoryInfo imagePathRoot) => this.GetFiles(imagePathRoot).Select(t => this.GetFileIndex(t)).ToArray();

		protected int AlternateIndex = 99999;

		protected DirectoryInfo GetDirectory(string imagePathRoot)
		{
			DirectoryInfo returnValue = null;

			returnValue = new(imagePathRoot);
			returnValue.Create();

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

		public async Task<IEnumerable<IStoredImage>> StoreLabelImagesAsync(string imagePathRoot, IEnumerable<IGetLabelResponse> labels)
		{
			IList<IStoredImage> returnValue = new List<IStoredImage>();

			//
			// Ensure the path exists.
			//
			DirectoryInfo dir = this.GetDirectory(imagePathRoot);

			//
			// Get the next ID.
			//
			int id = this.GetNextIndex(dir);

			foreach (IGetLabelResponse label in labels)
			{
				//
				// Get the file name.
				//
				string fileName = label.HasMultipleLabels ? this.FileName(dir, id, label.LabelIndex + 1) : this.FileName(dir, id);

				//
				// Write the image.
				//
				await File.WriteAllBytesAsync(fileName, label.Label);

				IStoredImage storedImage = new StoredImage()
				{
					Id = id,
					FullPath = fileName
				};

				//
				// Add the item to the list.
				//
				returnValue.Add(storedImage);
			}

			return returnValue;
		}

		public Task<IEnumerable<IStoredImage>> GetAllAsync(string imagePathRoot)
		{
			IList<IStoredImage> returnValue = new List<IStoredImage>();

			DirectoryInfo dir = this.GetDirectory(imagePathRoot);

			if (dir.Exists)
			{
				FileInfo[] files = this.GetFiles(dir);

				foreach (FileInfo file in files.OrderBy(t => t.CreationTime))
				{
					StoredImage si = new();

					try
					{
						si.Id = this.GetFileIndex(file);
					}
					catch
					{
						si.Id = AlternateIndex--;
					}

					si.FullPath = file.FullName;

					returnValue.Add(si);
				}
			}

			return Task.FromResult<IEnumerable<IStoredImage>>(returnValue.OrderBy(t => t.Timestamp).ToArray());
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
