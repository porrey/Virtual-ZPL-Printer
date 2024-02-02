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
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
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
			this.StartCommand = new DelegateCommand(async () => await this.StartCommandAsync(), () => !this.Running);
			this.CloseCommand = new DelegateCommand(async () => await this.CloseCommandAsync(), () => !this.Running);
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
				this.Text += $"Testing connectivity to '{this.LabelService.BaseUrl}'";
				await Task.Delay(250);

				//
				// Parse the URL.
				//
				Uri uri = new(this.LabelService.BaseUrl);

				//
				// Check DNS.
				//
				this.Text += $"{Environment.NewLine}Getting IP address for host name '{uri.DnsSafeHost}";
				IPHostEntry hostEntry = Dns.GetHostEntry(uri.DnsSafeHost);

				if (hostEntry.AddressList.Length != 0)
				{
					this.Text += $"{Environment.NewLine}Found {hostEntry.AddressList.Length} address for host name [OK]";

					foreach (IPAddress ip in hostEntry.AddressList)
					{
						this.Text += $"{Environment.NewLine}IP address '{ip}' was found for host name '{uri.DnsSafeHost}' [OK]";
						await Task.Delay(250);
					}

					//
					// Check port connectivity.
					//
					foreach (IPAddress ip in hostEntry.AddressList)
					{
						this.Text += $"{Environment.NewLine}Checking port '{uri.Port}' connectivity for IP address for host name '{ip}'";

						using (TcpClient client = new())
						{
							try
							{
								client.Connect(new IPEndPoint(ip, uri.Port));
								this.Text += $"{Environment.NewLine}Connection to port '{uri.Port}' was successful [OK]";
							}
							catch
							{
								this.Text += $"{Environment.NewLine}Connection to port '{uri.Port}' was unsuccessful [FAILED]";
							}
						}

						await Task.Delay(250);
					}

					//
					// Check web method connection
					//
					this.Text += $"{Environment.NewLine}Checking '{uri.Scheme}' connection";

					using (HttpClient client = new())
					{
						HttpResponseMessage response = await client.GetAsync(uri.OriginalString);

						if (response.StatusCode == HttpStatusCode.MethodNotAllowed)
						{
							this.Text += $"{Environment.NewLine}'{uri.Scheme}' connection was successful [OK]";
						}
						else
						{
							this.Text += $"{Environment.NewLine}'{uri.Scheme}' connection was unsuccessful [FAILED]";
						}
					}
				}
				else
				{
					this.Text += $"{Environment.NewLine}DNS lookup failed [FAILED]";
				}

				this.Text += $"{Environment.NewLine}Test completed successfully";
			}
			catch (Exception ex)
			{
				this.Text += $"{Environment.NewLine}{ex.Message}";
				this.Text += $"{Environment.NewLine}Test completed with one or more failures";
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
