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
using System.Threading.Tasks;
using ImageCache.Abstractions;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using VirtualPrinter.Events;

namespace VirtualPrinter.ViewModels
{
	public class StoredImageViewModel : BindableBase
	{
		public StoredImageViewModel(IEventAggregator eventAggregator, IStoredImage storedImage)
		{
			this.EventAggregator = eventAggregator;
			this.StoredImage = storedImage;
			this.ViewMetaDataCommand = new(() => _ = this.ViewMetaDataAsync(), () => true);
			this.ViewImageCommand = new(() => _ = this.ViewImageAsync(), () => true);
		}

		public DelegateCommand ViewMetaDataCommand { get; set; }
		public DelegateCommand ViewImageCommand { get; set; }
		protected IEventAggregator EventAggregator { get; set; }

		private IStoredImage _storedImage = null;
		public IStoredImage StoredImage
		{
			get
			{
				return this._storedImage;
			}
			set
			{
				this.SetProperty(ref this._storedImage, value);
			}
		}

		private Task ViewMetaDataAsync()
		{
			this.EventAggregator.GetEvent<Events.ViewMetaDataEvent>().Publish(new() { Item = this, Action = ViewMetaDataArgs.ActionType.Warnings });
			return Task.CompletedTask;
		}

		private Task ViewImageAsync()
		{
			this.EventAggregator.GetEvent<Events.ViewMetaDataEvent>().Publish(new() { Item = this, Action = ViewMetaDataArgs.ActionType.Image });
			return Task.CompletedTask;
		}
	}
}
