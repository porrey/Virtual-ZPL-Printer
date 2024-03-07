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
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VirtualPrinter.ApplicationSettings;

namespace ImageCache.Repository
{
	public class ImageCacheRepository(ILogger<ImageCacheRepository> logger, ISettings settings) : IImageCacheRepository
	{
		protected ILogger<ImageCacheRepository> Logger { get; set; } = logger;
		protected ISettings Settings { get; set; } = settings;

		protected static string FileName(DirectoryInfo imagePathRoot, string baseName, int id) => $@"{imagePathRoot.FullName}\{Path.GetFileNameWithoutExtension(baseName)}-{id}.png";
		protected static string FileName(DirectoryInfo imagePathRoot, string baseName, int id, int page) => $@"{imagePathRoot.FullName}\{Path.GetFileNameWithoutExtension(baseName)}-{id}-Page{page}.png";
		public static string MetaDataFile(string imageFile) => $"{Path.GetDirectoryName(imageFile)}/{Path.GetFileNameWithoutExtension(imageFile)}.json";
		protected object LockObject { get; } = new object();
		protected int AlternateIndex = 99999;

		protected IEnumerable<FileInfo> GetFiles(DirectoryInfo imagePathRoot)
		{
			IEnumerable<FileInfo> returnValue = null;

			this.Logger.LogDebug("Getting file all PNG files in '{name}'.", imagePathRoot.FullName);

			returnValue = [.. (from tbl in imagePathRoot.EnumerateFiles("*.png")
						   orderby tbl.CreationTime ascending
						   select tbl)];

			return returnValue;
		}

		protected int[] GetFileIndices(DirectoryInfo imagePathRoot)
		{
			int[] returnValue = [];

			this.Logger.LogDebug("Getting file indices for all PNG files in '{name}'.", imagePathRoot.FullName);

			returnValue = [..(from tbl in imagePathRoot.EnumerateFiles("*.png")
						   orderby tbl.CreationTime
						   select this.GetFileIndex(tbl))];

			return returnValue;
		}

		protected int GetFileIndex(FileInfo file)
		{
			int returnValue = 1;

			try
			{
				//
				// Split the file name.
				//
				this.Logger.LogDebug("Parsing file name '{name}' to get index.", file.Name);
				string[] parts = Path.GetFileNameWithoutExtension(file.Name).Split(new char[] { '-' }, StringSplitOptions.TrimEntries & StringSplitOptions.RemoveEmptyEntries);

				if (parts.Last().Contains("Page"))
				{
					returnValue = Convert.ToInt32(parts[^2]);
					this.Logger.LogDebug("File index is {index}.", returnValue);
				}
				else
				{
					returnValue = Convert.ToInt32(parts.Last());
					this.Logger.LogDebug("File index is {index}.", returnValue);
				}
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, "Exception parsing file index.");
			}

			return returnValue;
		}

		protected DirectoryInfo GetDirectory(string imagePathRoot)
		{
			DirectoryInfo returnValue = new(imagePathRoot);

			this.Logger.LogDebug("Ensuring file directory '{name}' exists.", imagePathRoot);
			returnValue.Create();

			return returnValue;
		}

		protected int GetNextIndex(DirectoryInfo imagePathRoot)
		{
			int returnValue = 1;

			this.Logger.LogDebug("Getting next index in '{name}'.", imagePathRoot);
			int[] indices = this.GetFileIndices(imagePathRoot);

			if (indices.Length != 0)
			{
				returnValue = indices.Max() + 1;
			}

			return returnValue;
		}

		public Task<IEnumerable<IStoredImage>> GetAllAsync(string imagePathRoot)
		{
			IList<IStoredImage> returnValue = [];

			this.Logger.LogDebug("Loading all PNG images from '{name}'.", imagePathRoot);
			DirectoryInfo dir = this.GetDirectory(imagePathRoot);

			if (dir.Exists)
			{
				IEnumerable<FileInfo> files = this.GetFiles(dir);

				foreach (FileInfo file in files.OrderBy(t => t.CreationTime))
				{
					StoredImage si = new();

					try
					{
						si.Id = this.GetFileIndex(file);
					}
					catch
					{
						si.Id = this.AlternateIndex--;
					}

					si.FullPath = file.FullName;

					returnValue.Add(si);
				}
			}

			return Task.FromResult<IEnumerable<IStoredImage>>(returnValue);
		}

		public Task<IEnumerable<IStoredImage>> StoreLabelImagesAsync(string imagePathRoot, IEnumerable<IGetLabelResponse> labels)
		{
			IList<IStoredImage> returnValue = [];

			this.Logger.LogDebug("Storing {count}  image(s) in {name}.", labels.Count(), imagePathRoot);

			lock (this.LockObject)
			{
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
					string fileName = label.HasMultipleLabels ? FileName(dir, label.ImageFileName, id, label.LabelIndex + 1) : FileName(dir, label.ImageFileName, id);
					this.Logger.LogDebug("Storing image to '{name}'.", fileName);

					//
					// Write the image.
					//
					_ = File.WriteAllBytesAsync(fileName, label.Label);

					//
					// Write a text file if the image has warnings.
					//
					if (label.Warnings != null && label.Warnings.Any())
					{
						string json = JsonConvert.SerializeObject(label, Formatting.Indented);
						_ = File.WriteAllTextAsync(MetaDataFile(fileName), json);
					}

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
			}

			return Task.FromResult<IEnumerable<IStoredImage>>(returnValue);
		}

		public Task<bool> ClearAllAsync(string imagePathRoot)
		{
			bool returnValue = false;

			this.Logger.LogDebug("Clearing ALL  image(s) in {name}.", imagePathRoot);

			DirectoryInfo dir = this.GetDirectory(imagePathRoot);
			int errorCount = 0;

			if (dir.Exists)
			{
				IEnumerable<FileInfo> files = this.GetFiles(dir);

				foreach (FileInfo file in files)
				{
					try
					{
						this.Logger.LogDebug("Attempting to delete file '{name}'.", file.Name);
						file.Delete();

						//
						// Clear the text file too.
						//
						if (File.Exists(MetaDataFile(file.FullName)))
						{
							File.Delete(MetaDataFile(file.FullName));
						}
					}
					catch (Exception ex)
					{
						this.Logger.LogError(ex, "Exception while deleting file '{name}'.", file.Name);
						errorCount++;
					}
				}

				returnValue = errorCount == 0;
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
					try
					{
						this.Logger.LogDebug("Attempting to delete file '{name}'.", file.Name);
						file.Delete();

						//
						// Clear the text file too.
						//
						if (File.Exists(MetaDataFile(file.FullName)))
						{
							File.Delete(MetaDataFile(file.FullName));
						}

						returnValue = true;
					}
					catch (Exception ex)
					{
						this.Logger.LogError(ex, "Exception while deleting file '{name}'.", file.Name);
					}
				}
			}

			return Task.FromResult(returnValue);
		}
	}
}
