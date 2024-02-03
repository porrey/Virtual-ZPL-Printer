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
using Labelary.Abstractions;
using Prism.Commands;
using Prism.Mvvm;

namespace VirtualPrinter.ViewModels
{
	public class GlobalSettingsViewModel : BindableBase
	{
		public GlobalSettingsViewModel(ILabelServiceConfiguration labelServiceConfiguration)
			: base()
		{
			this.LabelServiceConfiguration = labelServiceConfiguration;
			this.OkCommand = new(async () => await this.OkCommandAsync(), () => true);
			this.CancelCommand = new(async () => await this.CancelCommandAsync(), () => true);
		}

		protected ILabelServiceConfiguration LabelServiceConfiguration { get; set; }

		public DelegateCommand OkCommand { get; set; }
		public DelegateCommand CancelCommand { get; set; }

		private int _receiveTimeout = 1000;
		public int ReceiveTimeout
		{
			get
			{
				return this._receiveTimeout;
			}
			set
			{
				this.SetProperty(ref this._receiveTimeout, value);
			}
		}

		private int _sendTimeout = 1000;
		public int SendTimeout
		{
			get
			{
				return this._sendTimeout;
			}
			set
			{
				this.SetProperty(ref this._sendTimeout, value);
			}
		}

		private int _receiveBufferSize = -1;
		public int ReceiveBufferSize
		{
			get
			{
				return this._receiveBufferSize;
			}
			set
			{
				this.SetProperty(ref this._receiveBufferSize, value);
			}
		}

		private int _sendBufferSize = -1;
		public int SendBufferSize
		{
			get
			{
				return this._sendBufferSize;
			}
			set
			{
				this.SetProperty(ref this._sendBufferSize, value);
			}
		}

		private bool _noDelay = true;
		public bool NoDelay
		{
			get
			{
				return this._noDelay;
			}
			set
			{
				this.SetProperty(ref this._noDelay, value);
			}
		}

		private bool _linger = false;
		public bool Linger
		{
			get
			{
				return this._linger;
			}
			set
			{
				this.SetProperty(ref this._linger, value);
			}
		}

		private int _lingerTime = 0;
		public int LingerTime
		{
			get
			{
				return this._lingerTime;
			}
			set
			{
				this.SetProperty(ref this._lingerTime, value);
			}
		}

		private string _receivedDataEncoding = "utf-8";
		public string ReceivedDataEncoding
		{
			get
			{
				return this._receivedDataEncoding;
			}
			set
			{
				this.SetProperty(ref this._receivedDataEncoding, value);
			}
		}

		private string _apiUrl = null;
		public string ApiUrl
		{
			get
			{
				return this._apiUrl;
			}
			set
			{
				this.SetProperty(ref this._apiUrl, value);
			}
		}

		private string _apiMethod = null;
		public string ApiMethod
		{
			get
			{
				return this._apiMethod;
			}
			set
			{
				this.SetProperty(ref this._apiMethod, value);
			}
		}

		private bool _apiLinting = false;
		public bool ApiLinting
		{
			get
			{
				return this._apiLinting;
			}
			set
			{
				this.SetProperty(ref this._apiLinting, value);
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
			this.ReceivedDataEncoding = Properties.Settings.Default.ReceivedDataEncoding;
			this.ApiUrl = Properties.Settings.Default.ApiUrl;
			this.ApiMethod = Properties.Settings.Default.ApiMethod;
			this.ApiLinting = Properties.Settings.Default.ApiLinting;

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
			Properties.Settings.Default.ReceivedDataEncoding = this.ReceivedDataEncoding;

			Properties.Settings.Default.ApiUrl = this.ApiUrl;
			this.LabelServiceConfiguration.BaseUrl = this.ApiUrl;

			Properties.Settings.Default.ApiMethod = this.ApiMethod;
			this.LabelServiceConfiguration.Method = this.ApiMethod;

			Properties.Settings.Default.ApiLinting = this.ApiLinting;
			this.LabelServiceConfiguration.Linting = this.ApiLinting;

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
