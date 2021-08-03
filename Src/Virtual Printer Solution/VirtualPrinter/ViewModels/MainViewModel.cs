using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ImageCache.Abstractions;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using VirtualZplPrinter.Events;
using VirtualZplPrinter.Models;

namespace VirtualZplPrinter.ViewModels
{
	public class MainViewModel : BindableBase
	{
		public MainViewModel(IEventAggregator eventAggregator, IImageCacheRepository imageCacheRepository)
		{
			this.EventAggregator = eventAggregator;
			this.ImageCacheRepository = imageCacheRepository;

			this.Resolutions.Add(new Resolution() { Dpmm = 6 });
			this.Resolutions.Add(new Resolution() { Dpmm = 8 });
			this.Resolutions.Add(new Resolution() { Dpmm = 12 });
			this.Resolutions.Add(new Resolution() { Dpmm = 24 });
			this.SelectedResolution = this.Resolutions.ElementAt(1);

			this.LabelUnits.Add(new LabelUnit() { Unit = UnitsNet.Units.LengthUnit.Inch });
			this.LabelUnits.Add(new LabelUnit() { Unit = UnitsNet.Units.LengthUnit.Millimeter });
			this.LabelUnits.Add(new LabelUnit() { Unit = UnitsNet.Units.LengthUnit.Centimeter });
			this.SelectedLabelUnit = this.LabelUnits.ElementAt(0);

			this.StartCommand = new DelegateCommand(() => _ = this.StartAsync(), () => !this.IsBusy && !this.IsRunning && this.Port > 0 && this.LabelWidth > 0 && this.LabelHeight > 0);
			this.StopCommand = new DelegateCommand(() => _ = this.StopAsync(), () => !this.IsBusy && this.IsRunning);
			this.TestLabelCommand = new DelegateCommand(() => _ = this.TestLabelAsync(), () => !this.IsBusy && this.IsRunning);
			this.ClearLabelsCommand = new DelegateCommand(() => _ = this.ClearLabelsAsync(), () => !this.IsBusy && this.Labels.Count > 0);

			//
			// Subscribe to the running state changed event to update the running
			// status of the UI.
			//
			_ = this.EventAggregator.GetEvent<RunningStateChangedEvent>().Subscribe((a) => this.IsRunning = a.IsRunning, ThreadOption.UIThread);

			//
			// Subscribe to the label created event to add all new labels
			// to the UI.
			//
			_ = this.EventAggregator.GetEvent<LabelCreatedEvent>().Subscribe((a) =>
			  {
				  //
				  // Add the new label to the collection.
				  //
				  this.Labels.Add(a.Label);

				  //
				  // Make the new label the currently selected label.
				  //
				  this.SelectedLabel = a.Label;
			  }, ThreadOption.UIThread);

			//
			// If the image path has not been initialized, set to the documents folder.
			//
			if (this.ImagePath == null)
			{
				this.ImagePath = this.ImageCacheRepository.DefaultFolder;
			}
		}

		protected Random Rnd { get; } = new Random();
		protected IEventAggregator EventAggregator { get; set; }
		protected IImageCacheRepository ImageCacheRepository { get; set; }
		protected CancellationTokenSource TokenSource { get; set; }

		public ObservableCollection<Resolution> Resolutions { get; } = new ObservableCollection<Resolution>();
		public ObservableCollection<LabelUnit> LabelUnits { get; } = new ObservableCollection<LabelUnit>();
		public ObservableCollection<IStoredImage> Labels { get; } = new ObservableCollection<IStoredImage>();
		public DelegateCommand StartCommand { get; set; }
		public DelegateCommand StopCommand { get; set; }
		public DelegateCommand TestLabelCommand { get; set; }
		public DelegateCommand ClearLabelsCommand { get; set; }

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

		private LabelUnit _selectedLabelUnit = null;
		public LabelUnit SelectedLabelUnit
		{
			get
			{
				return _selectedLabelUnit;
			}
			set
			{
				this.SetProperty(ref _selectedLabelUnit, value);
			}
		}

		private IStoredImage _selectedLabel = null;
		public IStoredImage SelectedLabel
		{
			get
			{
				return _selectedLabel;
			}
			set
			{
				this.SetProperty(ref _selectedLabel, value);

				if (this.SelectedLabel != null)
				{
					this.StatusText = $"Viewing label {this.SelectedLabel.Id} of {this.Labels.Count}";
				}
				else
				{
					if (this.Labels.Any())
					{
						this.StatusText = $"{this.Labels.Count} label(s)";
					}
					else
					{
						this.StatusText = "No selection";
					}
				}

				this.RefreshCommands();
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

		private int _port = 0;
		public int Port
		{
			get
			{
				return _port;
			}
			set
			{
				this.SetProperty(ref _port, value);
				this.RefreshCommands();
			}
		}

		private double _lableWidth = 0;
		public double LabelWidth
		{
			get
			{
				return _lableWidth;
			}
			set
			{
				this.SetProperty(ref _lableWidth, value);
				this.RefreshCommands();
			}
		}

		private double _lableHeight = 0;
		public double LabelHeight
		{
			get
			{
				return _lableHeight;
			}
			set
			{
				this.SetProperty(ref _lableHeight, value);
				this.RefreshCommands();
			}
		}

		private string _statusText = "No selection";
		public string StatusText
		{
			get
			{
				return _statusText;
			}
			set
			{
				this.SetProperty(ref _statusText, value);
			}
		}

		private bool _autoStart = false;
		public bool AutoStart
		{
			get
			{
				return _autoStart;
			}
			set
			{
				this.SetProperty(ref _autoStart, value);
				this.RefreshCommands();
			}
		}

		private string _imagePath = null;
		public string ImagePath
		{
			get
			{
				return _imagePath;
			}
			set
			{
				this.SetProperty(ref _imagePath, value);
				_ = this.LoadLabelsAsync();
			}
		}

		public async Task InitializeAsync()
		{
			//
			// Auto start
			//
			if (this.AutoStart && this.Port > 0)
			{
				await Task.Delay(250);
				_ = this.StartAsync();
			}
		}

		public string MyIpAddress
		{
			get
			{
				//
				// Gets the machine IP address and formats it into a displayable value.
				//
				IPHostEntry entry = Dns.GetHostEntry(Dns.GetHostName());
				IPAddress a = entry.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
				return a.ToString();
			}
		}

		protected void RefreshCommands()
		{
			//
			// refresh the state of all of the command buttons.
			//
			this.StartCommand.RaiseCanExecuteChanged();
			this.StopCommand.RaiseCanExecuteChanged();
			this.TestLabelCommand.RaiseCanExecuteChanged();
			this.ClearLabelsCommand.RaiseCanExecuteChanged();
		}

		protected Task StartAsync()
		{
			try
			{
				//
				// Publish an event to start the listener. The arguments of the message
				// specify the label size, DPMM of the label, TCP port and the folder
				// location to use for label storage.
				//
				this.EventAggregator.GetEvent<StartEvent>().Publish(new StartEventArgs()
				{
					LabelConfiguration = new LabelConfiguration()
					{
						Dpmm = this.SelectedResolution.Dpmm,
						LabelHeight = this.LabelHeight,
						LabelWidth = this.LabelWidth,
						Unit = this.SelectedLabelUnit.Unit
					},
					Port = this.Port,
					ImagePath = this.ImagePath
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}

			return Task.CompletedTask;
		}

		protected Task StopAsync()
		{
			try
			{
				//
				// Publish an event to stop the listener.
				//
				this.EventAggregator.GetEvent<StopEvent>().Publish(new StopEventArgs() { });
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}

			return Task.CompletedTask;
		}

		protected async Task TestLabelAsync()
		{
			try
			{
				using (TcpClient client = new())
				{
					//
					// Connect to the local host.
					//
					await client.ConnectAsync("127.0.0.1", this.Port);

					//
					// Create a stream to send th ZPL.
					//
					using (Stream stream = client.GetStream())
					{
						//
						// Create a random bar code value for the label.
						//
						int id = this.Rnd.Next(1, 99999999);

						//
						// Read the sample ZPL.
						//
						string zpl = File.ReadAllText("./samples/6x4-203dpi.txt"); ;

						//
						// Convert the ZPL to a byte array.
						//
						byte[] buffer = ASCIIEncoding.UTF8.GetBytes(zpl.Replace("{id}", id.ToString("00000000")));

						//
						// Send the ZPL string.
						//
						await stream.WriteAsync(buffer.AsMemory(0, buffer.Length));

						//
						// Close the connection.
						//
						client.Close();
					}
				}
			}
			catch (Exception ex)
			{
				this.StatusText = $"Error: {ex.Message}";
			}
		}

		protected async Task ClearLabelsAsync()
		{
			try
			{
				//
				// Clear the collection.
				//
				this.Labels.Clear();

				//
				// Set the selected label to null.
				//
				this.SelectedLabel = null;

				//
				// Remove the labels from storage.
				//
				_ = await this.ImageCacheRepository.ClearAllAsync(this.ImagePath);
			}
			catch (Exception ex)
			{
				this.StatusText = $"Error: {ex.Message}";
			}
			finally
			{
				this.RefreshCommands();
			}
		}

		protected async Task LoadLabelsAsync()
		{
			try
			{
				//
				// Clea the current list.
				//
				this.Labels.Clear();

				//
				// Load the previous labels
				//
				IEnumerable<IStoredImage> labels = await this.ImageCacheRepository.GetAllAsync(this.ImagePath);

				//
				// Add the labels to the collection.
				//
				foreach (IStoredImage label in labels)
				{
					this.Labels.Add(label);
				}

				//
				// Select the last label.
				//
				if (this.Labels.Any())
				{
					this.SelectedLabel = this.Labels.Last();
				}
			}
			catch (Exception ex)
			{
				this.StatusText = $"Error: {ex.Message}";
			}
			finally
			{
				this.RefreshCommands();
			}
		}
	}
}
