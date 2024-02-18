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
				return this._id;
			}
			set
			{
				this.SetProperty(ref this._id, value);
			}
		}

		private string _fullPath = null;
		public string FullPath
		{
			get
			{
				return this._fullPath;
			}
			set
			{
				this.SetProperty(ref this._fullPath, value);

				this.Timestamp = File.GetCreationTime(this.FullPath);
				this.ActualTime = this.Timestamp.ToString("h:mm:ss.fff tt");
				this.RaisePropertyChanged(nameof(this.MetaDataFile));
				this.RaisePropertyChanged(nameof(this.HasMetaData));

				try
				{
					this.DisplayLabel = this.Timestamp.Humanize();
				}
				catch
				{
					this.DisplayLabel = this.Timestamp.ToString();
				}
			}
		}

		public string MetaDataFile => ImageCacheRepository.MetaDataFile(this.FullPath);
		public bool HasMetaData => File.Exists(this.MetaDataFile);

		private DateTime _timestamp = DateTime.MinValue;
		public DateTime Timestamp
		{
			get
			{
				return this._timestamp;
			}
			set
			{
				this.SetProperty(ref this._timestamp, value);
			}
		}

		private string _actualTime = null;
		public string ActualTime
		{
			get
			{
				return this._actualTime;
			}
			set
			{
				this.SetProperty(ref this._actualTime, value);
			}
		}

		private string _displayLabel = null;
		public string DisplayLabel
		{
			get
			{
				return this._displayLabel;
			}
			set
			{
				this.SetProperty(ref this._displayLabel, value);
			}
		}

		public string Information
		{
			get
			{
				string returnValue = Path.GetFileName(this.FullPath);

				if (this.HasMetaData)
				{
					returnValue = $"{returnValue} [The ZPL contains warnings]";
				}

				return returnValue;
			}
		}

		public void Refresh()
		{
			this.FullPath = this._fullPath;
		}
	}
}
