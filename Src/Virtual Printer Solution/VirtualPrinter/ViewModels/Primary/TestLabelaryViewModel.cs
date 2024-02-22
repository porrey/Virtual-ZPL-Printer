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
using System.Net;
using System.Net.Sockets;
using Labelary.Abstractions;
using Prism.Commands;
using Prism.Mvvm;

namespace VirtualPrinter.ViewModels
{
	public class TestLabelaryViewModel : BindableBase
	{
		public TestLabelaryViewModel(ILabelService labelService)
			: base()
		{
			this.LabelService = labelService;
			this.StartCommand = new(async () => await this.StartCommandAsync(), () => !this.Running);
			this.CloseCommand = new(async () => await this.CloseCommandAsync(), () => !this.Running);
		}

		protected ILabelService LabelService { get; set; }

		public DelegateCommand StartCommand { get; set; }
		public DelegateCommand CloseCommand { get; set; }

		private string _text = null;
		public string Text
		{
			get
			{
				return this._text;
			}
			set
			{
				this.SetProperty(ref this._text, value);
			}
		}

		private bool _running = false;
		public bool Running
		{
			get
			{
				return this._running;
			}
			set
			{
				this.SetProperty(ref this._running, value);
				this.RefreshCommands();
			}
		}

		public Task InitializeAsync()
		{
			this.RefreshCommands();
			return Task.CompletedTask;
		}

		protected async Task StartCommandAsync()
		{
			try
			{
				this.Running = true;
				this.Text = string.Empty;
				this.Text += string.Format(Properties.Strings.Connectivity_Test_Message_Start, this.LabelService.LabelServiceConfiguration.BaseUrl);
				await Task.Delay(250);

				//
				// Parse the URL.
				//
				Uri uri = new(this.LabelService.LabelServiceConfiguration.BaseUrl);

				//
				// Check DNS.
				//
				this.Text += string.Format(Properties.Strings.Connectivity_Test_Message_Dns, Environment.NewLine, uri.DnsSafeHost);
				IPHostEntry hostEntry = Dns.GetHostEntry(uri.DnsSafeHost);

				if (hostEntry.AddressList.Length != 0)
				{
					this.Text += string.Format(Properties.Strings.Connectivity_Test_Message_Host, Environment.NewLine, hostEntry.AddressList.Length);

					foreach (IPAddress ip in hostEntry.AddressList)
					{
						this.Text += string.Format(Properties.Strings.Connectivity_Test_Message_Ip, Environment.NewLine, ip, uri.DnsSafeHost);
						await Task.Delay(250);
					}

					//
					// Check port connectivity.
					//
					foreach (IPAddress ip in hostEntry.AddressList)
					{
						this.Text += string.Format(Properties.Strings.Connectivity_Test_Message_PortTest, Environment.NewLine, uri.Port, ip);

						using (TcpClient client = new())
						{
							try
							{
								client.Connect(new IPEndPoint(ip, uri.Port));
								this.Text += string.Format(Properties.Strings.Connectivity_Test_Message_PortTest_Success, Environment.NewLine, uri.Port);
							}
							catch
							{
								this.Text += string.Format(Properties.Strings.Connectivity_Test_Message_PortTest_Failed, Environment.NewLine, uri.Port);
							}
						}

						await Task.Delay(250);
					}

					//
					// Check web method connection
					//
					this.Text += string.Format(Properties.Strings.Connectivity_Test_Message_WebMethodTest, Environment.NewLine, uri.Scheme.ToUpper(), this.LabelService.LabelServiceConfiguration.Method);

					//
					// Create a basic configuration.
					//
					ILabelConfiguration labelConfiguration = new LabelConfiguration()
					{
						Dpmm = 8,
						LabelFilters = [],
						LabelHeight = 6,
						LabelWidth = 4,
						Unit = UnitsNet.Units.LengthUnit.Inch,
						LabelRotation = 0
					};

					//
					// Call to get a very basic label.
					//
					IGetLabelResponse response = await this.LabelService.GetLabelAsync(labelConfiguration, $"^XA\r\n^FO10,10^FD{Properties.Strings.Connectivity_Test_Label_Text}^FS\r\n^XZ", 0);

					if (response != null && response.Result)
					{
						this.Text += string.Format(Properties.Strings.Connectivity_Test_Message_WebMethodTest_Success, Environment.NewLine, uri.Scheme.ToUpper(), this.LabelService.LabelServiceConfiguration.Method);
					}
					else
					{
						this.Text += string.Format(Properties.Strings.Connectivity_Test_Message_WebMethod_Failed, Environment.NewLine, uri.Scheme.ToUpper(), this.LabelService.LabelServiceConfiguration.Method);
					}
				}
				else
				{
					this.Text += string.Format(Properties.Strings.Connectivity_Test_Message_Dns_Failed, Environment.NewLine);
				}

				this.Text += string.Format(Properties.Strings.Connectivity_Test_Success, Environment.NewLine);
			}
			catch (Exception ex)
			{
				this.Text += $"{Environment.NewLine}{ex.Message}";
				this.Text += string.Format(Properties.Strings.Connectivity_Test_Failed, Environment.NewLine);
			}
			finally
			{
				this.Running = false;
			}
		}

		protected Task CloseCommandAsync()
		{
			return Task.CompletedTask;
		}

		public void RefreshCommands()
		{
			//
			// Refresh the state of all of the command buttons.
			//
			this.StartCommand.RaiseCanExecuteChanged();
			this.CloseCommand.RaiseCanExecuteChanged();
		}
	}
}
