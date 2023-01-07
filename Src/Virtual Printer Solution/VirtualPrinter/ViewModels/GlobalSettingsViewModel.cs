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
using Prism.Commands;
using Prism.Mvvm;

namespace VirtualPrinter.ViewModels
{
	public class GlobalSettingsViewModel : BindableBase
	{
		public GlobalSettingsViewModel()
			: base()
		{
			this.OkCommand = new DelegateCommand(async () => await this.OkCommandAsync(), () => true);
			this.CancelCommand = new DelegateCommand(async () => await this.CancelCommandAsync(), () => true);
		}

		public DelegateCommand OkCommand { get; set; }
		public DelegateCommand CancelCommand { get; set; }

		private int _receiveTimeout = 1000;
		public int ReceiveTimeout
		{
			get
			{
				return _receiveTimeout;
			}
			set
			{
				this.SetProperty(ref _receiveTimeout, value);
			}
		}

		private int _sendTimeout = 1000;
		public int SendTimeout
		{
			get
			{
				return _sendTimeout;
			}
			set
			{
				this.SetProperty(ref _sendTimeout, value);
			}
		}

		private int _receiveBufferSize = -1;
		public int ReceiveBufferSize
		{
			get
			{
				return _receiveBufferSize;
			}
			set
			{
				this.SetProperty(ref _receiveBufferSize, value);
			}
		}

		private int _sendBufferSize = -1;
		public int SendBufferSize
		{
			get
			{
				return _sendBufferSize;
			}
			set
			{
				this.SetProperty(ref _sendBufferSize, value);
			}
		}

		private bool _noDelay = true;
		public bool NoDelay
		{
			get
			{
				return _noDelay;
			}
			set
			{
				this.SetProperty(ref _noDelay, value);
			}
		}

		private bool _linger = false;
		public bool Linger
		{
			get
			{
				return _linger;
			}
			set
			{
				this.SetProperty(ref _linger, value);
			}
		}

		private int _lingerTime = 0;
		public int LingerTime
		{
			get
			{
				return _lingerTime;
			}
			set
			{
				this.SetProperty(ref _lingerTime, value);
			}
		}

		public Task InitializeAsync()
		{
			this.ReceiveTimeout = Properties.Settings.Default.ReceiveTimeout;
			this.SendTimeout = Properties.Settings.Default.SendTimeout;
			this.ReceiveBufferSize = Properties.Settings.Default.ReceiveBufferSize;
			this.SendBufferSize = Properties.Settings.Default.SendBufferSize;
			this.NoDelay = Properties.Settings.Default.NoDelay;
			this.Linger = Properties.Settings.Default.Linger;
			this.LingerTime = Properties.Settings.Default.LingerTime;

			this.RefreshCommands();
			return Task.CompletedTask;
		}

		protected Task OkCommandAsync()
		{
			Properties.Settings.Default.ReceiveTimeout = this.ReceiveTimeout;
			Properties.Settings.Default.SendTimeout = this.SendTimeout;
			Properties.Settings.Default.ReceiveBufferSize = this.ReceiveBufferSize;
			Properties.Settings.Default.SendBufferSize = this.SendBufferSize;
			Properties.Settings.Default.NoDelay = this.NoDelay;
			Properties.Settings.Default.Linger = this.Linger;
			Properties.Settings.Default.LingerTime = this.LingerTime;

			return Task.CompletedTask;
		}

		protected Task CancelCommandAsync()
		{
			return Task.CompletedTask;
		}

		public void RefreshCommands()
		{
			//
			// Refresh the state of all of the command buttons.
			//
			this.OkCommand.RaiseCanExecuteChanged();
			this.CancelCommand.RaiseCanExecuteChanged();
		}
	}
}
