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
using System.Collections.ObjectModel;
using Labelary.Abstractions;
using Prism.Mvvm;

namespace VirtualPrinter.ViewModels
{
	public class ZplViewModel : BindableBase
	{
		public ZplViewModel()
			: base()
		{
		}

		public ObservableCollection<Warning> Warnings { get; } = [];

		public Task InitializeAsync()
		{
			return Task.CompletedTask;
		}

		private string _title = null;
		public string Title
		{
			get
			{
				return this._title;
			}
			set
			{
				this.SetProperty(ref this._title, value);
			}
		}

		private IGetLabelResponse _labelResponse = null;
		public IGetLabelResponse LabelResponse
		{
			get
			{
				return this._labelResponse;
			}
			set
			{
				this.SetProperty(ref this._labelResponse, value);
				this.Title = $"{Properties.Strings.ZPL_Warnings} ({this.LabelResponse.Warnings.Count()})";
				this.Zpl = this.LabelResponse.Zpl;
				this.Warnings.Clear();

				foreach (Warning warning in this.LabelResponse.Warnings)
				{
					this.Warnings.Add(warning);
				}
			}
		}

		private Warning _selectedWarning = null;
		public Warning SelectedWarning
		{
			get
			{
				return this._selectedWarning;
			}
			set
			{
				this.SetProperty(ref this._selectedWarning, value);
			}
		}

		private string _zpl = null;
		public string Zpl
		{
			get
			{
				return this._zpl;
			}
			set
			{
				this.SetProperty(ref this._zpl, value);
			}
		}
	}
}
