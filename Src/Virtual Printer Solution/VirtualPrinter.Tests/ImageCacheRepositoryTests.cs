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
using ImageCache.Abstractions;
using ImageCache.Repository;
using Labelary.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.IO;
using VirtualPrinter.ApplicationSettings;
using Xunit;

namespace VirtualPrinter.Tests
{
	public class ImageCacheRepositoryTests : IDisposable
	{
		private readonly string _testDirectory;
		private readonly ImageCacheRepository _repository;

		public ImageCacheRepositoryTests()
		{
			_testDirectory = Path.Combine(Path.GetTempPath(), $"VZPLTests_{Guid.NewGuid()}");
			Directory.CreateDirectory(_testDirectory);

			NullLogger<ImageCacheRepository> logger = new();
			Mock<ISettings> settings = new();
			settings.Setup(s => s.RootFolder).Returns(new DirectoryInfo(_testDirectory));

			_repository = new ImageCacheRepository(logger, settings.Object);
		}

		public void Dispose()
		{
			try
			{
				if (Directory.Exists(_testDirectory))
				{
					Directory.Delete(_testDirectory, recursive: true);
				}
			}
			catch { }
		}

		[Fact]
		public void MetaDataFile_ReturnsJsonPathForPngFile()
		{
			string imageFile = @"C:\images\label-1.png";
			string result = ImageCacheRepository.MetaDataFile(imageFile);
			Assert.EndsWith(".json", result);
			Assert.Contains("label-1", result);
		}

		[Fact]
		public async Task GetAllAsync_WithEmptyDirectory_ReturnsEmpty()
		{
			string dir = Path.Combine(_testDirectory, "empty");
			Directory.CreateDirectory(dir);

			IEnumerable<IStoredImage> result = await _repository.GetAllAsync(dir);

			Assert.Empty(result);
		}

		[Fact]
		public async Task ClearAllAsync_WithEmptyDirectory_ReturnsTrue()
		{
			string dir = Path.Combine(_testDirectory, "clear");
			Directory.CreateDirectory(dir);

			bool result = await _repository.ClearAllAsync(dir);

			Assert.True(result);
		}

		[Fact]
		public async Task StoreLabelImagesAsync_StoresImages()
		{
			string dir = Path.Combine(_testDirectory, "store");
			Directory.CreateDirectory(dir);

			byte[] pngData = [137, 80, 78, 71, 13, 10, 26, 10];
			GetLabelResponse label = new()
			{
				LabelIndex = 0,
				LabelCount = 1,
				Result = true,
				Label = pngData,
				ImageFileName = "test-label",
				Warnings = []
			};

			IEnumerable<IStoredImage> result = await _repository.StoreLabelImagesAsync(dir, [label]);

			await Task.Delay(100);

			Assert.Single(result);
			Assert.NotNull(result.First().FullPath);
		}

		[Fact]
		public async Task DeleteImageAsync_WithNonExistentFile_ReturnsFalse()
		{
			string dir = Path.Combine(_testDirectory, "delete");
			Directory.CreateDirectory(dir);

			bool result = await _repository.DeleteImageAsync(dir, "nonexistent.png");

			Assert.False(result);
		}
	}
}
