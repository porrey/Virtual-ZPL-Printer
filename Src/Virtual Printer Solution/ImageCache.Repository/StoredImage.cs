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
