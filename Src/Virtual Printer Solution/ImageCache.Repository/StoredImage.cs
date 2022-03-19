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
using Humanizer;
using ImageCache.Abstractions;
using Prism.Mvvm;

namespace ImageCache.Repository
{
	public class StoredImage : BindableBase, IStoredImage
	{
		private int _id = 0;
		public int Id
		{
			get
			{
				return _id;
			}
			set
			{
				this.SetProperty(ref _id, value);
			}
		}

		private string _fullPath = null;
		public string FullPath
		{
			get
			{
				return _fullPath;
			}
			set
			{
				this.SetProperty(ref _fullPath, value);

				this.Timestamp = File.GetCreationTime(this.FullPath);
				this.ActualTime = this.Timestamp.ToString("h:mm:ss.fff tt");
				this.DisplayLabel = this.Timestamp.Humanize();
			}
		}

		private DateTime _timestamp = DateTime.MinValue;
		public DateTime Timestamp
		{
			get
			{
				return _timestamp;
			}
			set
			{
				this.SetProperty(ref _timestamp, value);
			}
		}

		private string _actualTime = null;
		public string ActualTime
		{
			get
			{
				return _actualTime;
			}
			set
			{
				this.SetProperty(ref _actualTime, value);
			}
		}

		private string _displayLabel = null;
		public string DisplayLabel
		{
			get
			{
				return _displayLabel;
			}
			set
			{
				this.SetProperty(ref _displayLabel, value);
			}
		}

		public void Refresh()
		{
			this.FullPath = _fullPath;
		}
	}
}
