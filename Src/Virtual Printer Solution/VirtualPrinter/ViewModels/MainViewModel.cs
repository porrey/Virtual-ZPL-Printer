using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using VirtualPrinter.Models;

namespace VirtualPrinter.ViewModels
{
	public class MainViewModel : BindableBase
	{
		public MainViewModel()
		{
			this.Resolutions.Add(new Resolution() { Dpmm = 6 });
			this.Resolutions.Add(new Resolution() { Dpmm = 8 });
			this.Resolutions.Add(new Resolution() { Dpmm = 12 });
			this.Resolutions.Add(new Resolution() { Dpmm = 24 });
			this.SelectedResolution = this.Resolutions.ElementAt(1);

			this.StartCommand = new DelegateCommand(() => _ = this.StartAsync(), () => !this.IsBusy && !this.IsRunning);
			this.StopCommand = new DelegateCommand(() => _ = this.StopAsync(), () => !this.IsBusy && this.IsRunning);
		}

		public ObservableCollection<Resolution> Resolutions { get; } = new ObservableCollection<Resolution>();
		public DelegateCommand StartCommand { get; set; }
		public DelegateCommand StopCommand { get; set; }

		protected CancellationTokenSource TokenSource { get; set; }

		private Resolution _selectedResolution = null;
		public Resolution SelectedResolution
		{
			get
			{
				return _selectedResolution;
			}
			set
			{
				this.SetProperty(ref _selectedResolution, value);
			}
		}

		private bool _isBusy = false;
		public bool IsBusy
		{
			get
			{
				return _isBusy;
			}
			set
			{
				this.SetProperty(ref _isBusy, value);
				this.RefreshCommands();
			}
		}

		private bool _isRunning = false;
		public bool IsRunning
		{
			get
			{
				return _isRunning;
			}
			set
			{
				this.SetProperty(ref _isRunning, value);
				this.RefreshCommands();
			}
		}

		private int _port = 9100;
		public int Port
		{
			get
			{
				return _port;
			}
			set
			{
				this.SetProperty(ref _port, value);
			}
		}

		private int _lableWidth = 2;
		public int LableWidth
		{
			get
			{
				return _lableWidth;
			}
			set
			{
				this.SetProperty(ref _lableWidth, value);
			}
		}

		private int _lableHeight = 2;
		public int LableHeight
		{
			get
			{
				return _lableHeight;
			}
			set
			{
				this.SetProperty(ref _lableHeight, value);
			}
		}

		private string _imagePath = "Assets/carton-label.png";
		public string ImagePath
		{
			get
			{
				return _imagePath;
			}
			set
			{
				this.SetProperty(ref _imagePath, value);
			}
		}

		protected void RefreshCommands()
		{
			this.StartCommand.RaiseCanExecuteChanged();
			this.StopCommand.RaiseCanExecuteChanged();
		}

		protected Task StartAsync()
		{
			return Task.Factory.StartNew(async () =>
			{
				this.IsRunning = true;
				this.TokenSource = new CancellationTokenSource();

				TcpListener listener = new(IPAddress.Any, this.Port);
				listener.Start();

				while (!this.TokenSource.IsCancellationRequested)
				{
					try
					{
						//
						// Accept the connection.
						//
						TcpClient client = await listener.AcceptTcpClientAsync();

						//
						// Get the network stream.
						//
						NetworkStream stream = client.GetStream();

						while (client.Connected && stream.CanRead)
						{
							if (stream.DataAvailable)
							{
								//
								// Create a buffer for the data that is available.
								//
								byte[] buffer = new byte[client.Available];

								//
								// Read the data into the buffer.
								//
								await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));
								string zpl = ASCIIEncoding.UTF8.GetString(buffer);
								this.ImagePath = await this.GetLabel(zpl);
							}
						}
					}
					catch
					{
					}
				}
			});
		}

		protected Task StopAsync()
		{
			this.IsRunning = false;
			return Task.CompletedTask;
		}

		protected async Task<string> GetLabel(string zpl)
		{
			string returnValue = null;

			using (HttpClient client = new HttpClient())
			{
				using (StringContent content = new StringContent(zpl, Encoding.UTF8, "application/x-www-form-urlencoded"))
				{
					Console.WriteLine("Retrieving label...");
					using (HttpResponseMessage response = await client.PostAsync($"http://api.labelary.com/v1/printers/{this.SelectedResolution.Dpmm}dpmm/labels/{this.LableHeight}x{this.LableWidth}/0/", content))
					{
						if (response.IsSuccessStatusCode)
						{
							byte[] image = await response.Content.ReadAsByteArrayAsync();
							returnValue = $@"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\zpl-label-image.png";
							File.WriteAllBytes(returnValue, image);
						}
						else
						{
							throw new Exception(response.ReasonPhrase);
						}
					}
				}
			}

			return returnValue;
		}
	}
}
