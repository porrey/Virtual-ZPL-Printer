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

		public ObservableCollection<ApiMethodViewModel> ApiMethods { get; } = [];
		public ObservableCollection<TextEncodingViewModel> TextEncodings { get; } = [];

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

		private TextEncodingViewModel _receivedDataEncoding = null;
		public TextEncodingViewModel ReceivedDataEncoding
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

		private ApiMethodViewModel _apiMethod = null;
		public ApiMethodViewModel ApiMethod
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
				this.ApiMethods.Add(new ApiMethodViewModel() { DisplayName = Properties.Strings.Global_Settings_WebMethod_Post, Value = "POST" });
				this.ApiMethods.Add(new ApiMethodViewModel() { DisplayName = Properties.Strings.Global_Settings_WebMethod_Get, Value = "GET" });

				this.TextEncodings.Add(new TextEncodingViewModel() { DisplayName = "ASCII", Value = "ASCII" });
				this.TextEncodings.Add(new TextEncodingViewModel() { DisplayName = "UTF-7", Value = "UTF-7" });
				this.TextEncodings.Add(new TextEncodingViewModel() { DisplayName = "UTF-8", Value = "UTF-8" });
				this.TextEncodings.Add(new TextEncodingViewModel() { DisplayName = "UTF-32", Value = "UTF-32" });
				this.TextEncodings.Add(new TextEncodingViewModel() { DisplayName = "UTF-64", Value = "UTF-64" });
				this.TextEncodings.Add(new TextEncodingViewModel() { DisplayName = "ISO-8859-1", Value = "ISO-8859-1" });

				this.ReceiveTimeout = this.Settings.ReceiveTimeout;
				this.SendTimeout = this.Settings.SendTimeout;
				this.ReceiveBufferSize = this.Settings.ReceiveBufferSize;
				this.SendBufferSize = this.Settings.SendBufferSize;
				this.NoDelay = this.Settings.NoDelay;
				this.Linger = this.Settings.Linger;
				this.LingerTime = this.Settings.LingerTime;
				this.ReceivedDataEncoding = this.TextEncodings.Where(t => t.Value == this.Settings.ReceivedDataEncoding).FirstOrDefault();
				this.ApiUrl = this.Settings.ApiUrl;
				this.ApiMethod = this.ApiMethods.Where(t => t.Value == this.Settings.ApiMethod).FirstOrDefault();
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
				this.Settings.ReceivedDataEncoding = this.ReceivedDataEncoding?.Value;

				this.Settings.ApiUrl = this.ApiUrl;
				this.LabelServiceConfiguration.BaseUrl = this.ApiUrl;

				this.Settings.ApiMethod = this.ApiMethod?.Value;
				this.LabelServiceConfiguration.Method = this.ApiMethod?.Value;

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
