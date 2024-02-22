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
using System.Windows;
using Labelary.Abstractions;
using Prism.Commands;
using Prism.Mvvm;
using VirtualPrinter.ApplicationSettings;

namespace VirtualPrinter.ViewModels
{
	public class GlobalSettingsViewModel : BindableBase
	{
		public GlobalSettingsViewModel(ILabelServiceConfiguration labelServiceConfiguration, ISettings settings)
			: base()
		{
			this.LabelServiceConfiguration = labelServiceConfiguration;
			this.Settings = settings;

			this.OkCommand = new(async () => await this.OkCommandAsync(), () => true);
			this.CancelCommand = new(async () => await this.CancelCommandAsync(), () => true);
		}

		protected ILabelServiceConfiguration LabelServiceConfiguration { get; set; }
		protected ISettings Settings { get; set; }

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
			try
			{
				this.ReceiveTimeout = this.Settings.ReceiveTimeout;
				this.SendTimeout = this.Settings.SendTimeout;
				this.ReceiveBufferSize = this.Settings.ReceiveBufferSize;
				this.SendBufferSize = this.Settings.SendBufferSize;
				this.NoDelay = this.Settings.NoDelay;
				this.Linger = this.Settings.Linger;
				this.LingerTime = this.Settings.LingerTime;
				this.ReceivedDataEncoding = this.Settings.ReceivedDataEncoding;
				this.ApiUrl = this.Settings.ApiUrl;
				this.ApiMethod = this.Settings.ApiMethod;
				this.ApiLinting = this.Settings.ApiLinting;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Properties.Strings.MessageBox_Exception_Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}

			return Task.CompletedTask;
		}

		protected Task OkCommandAsync()
		{
			try
			{
				this.Settings.ReceiveTimeout = this.ReceiveTimeout;
				this.Settings.SendTimeout = this.SendTimeout;
				this.Settings.ReceiveBufferSize = this.ReceiveBufferSize;
				this.Settings.SendBufferSize = this.SendBufferSize;
				this.Settings.NoDelay = this.NoDelay;
				this.Settings.Linger = this.Linger;
				this.Settings.LingerTime = this.LingerTime;
				this.Settings.ReceivedDataEncoding = this.ReceivedDataEncoding;

				this.Settings.ApiUrl = this.ApiUrl;
				this.LabelServiceConfiguration.BaseUrl = this.ApiUrl;

				this.Settings.ApiMethod = this.ApiMethod;
				this.LabelServiceConfiguration.Method = this.ApiMethod;

				this.Settings.ApiLinting = this.ApiLinting;
				this.LabelServiceConfiguration.Linting = this.ApiLinting;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Properties.Strings.MessageBox_Exception_Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}

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
