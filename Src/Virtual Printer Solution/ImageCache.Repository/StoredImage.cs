using System;
using System.IO;
using ImageCache.Abstractions;

namespace ImageCache.Repository
{
	public class StoredImage : IStoredImage
	{
		public int Id { get; set; }
		public string FullPath { get; set; }
		public DateTime Timestamp => File.GetCreationTime(this.FullPath);

		public string DisplayLabel
		{
			get
			{
				string returnValue = string.Empty;

				if (this.Timestamp.Date == DateTime.Now.Date)
				{
					returnValue = $"Today at {this.Timestamp.ToShortTimeString()}";
				}
				else if (this.Timestamp.Date == DateTime.Now.Date.AddDays(-1))
				{
					returnValue = $"Yesterday at {this.Timestamp.ToShortTimeString()}";
				}
				else
				{
					int days = (int)DateTime.Now.Date.Subtract(this.Timestamp).TotalDays;
					returnValue = $"{days:#,###} Days Ago at {this.Timestamp.ToShortTimeString()}";
				}

				return returnValue;
			}
		}

		public string ActualTime => this.Timestamp.ToString("h:mm:ss.fff tt");
	}
}
