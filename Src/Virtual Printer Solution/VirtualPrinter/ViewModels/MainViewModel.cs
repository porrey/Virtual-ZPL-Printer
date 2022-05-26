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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using ImageCache.Abstractions;
using Labelary.Abstractions;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using UnitsNet;
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

			_ = this.LoadResolutions();
			_ = this.LoadRotations();
			_ = this.LoadIpAddresses();
			_ = this.LoadLabelUnits();

			this.StartCommand = new DelegateCommand(() => _ = this.StartAsync(), () => !this.IsBusy && !this.IsRunning && this.Port > 0 && this.LabelWidth > 0 && this.LabelHeight > 0);
			this.StopCommand = new DelegateCommand(() => _ = this.StopAsync(), () => !this.IsBusy && this.IsRunning);
			this.TestLabelCommand = new DelegateCommand(() => _ = this.TestLabelAsync(), () => !this.IsBusy && this.IsRunning);
			this.ClearLabelsCommand = new DelegateCommand(() => _ = this.ClearLabelsAsync(), () => !this.IsBusy && this.Labels.Count > 0);
			this.BrowseCommand = new DelegateCommand(() => _ = this.BrowseCommandAsync(), () => !this.IsBusy);
			this.DeleteLabelCommand = new DelegateCommand(() => _ = this.DeleteLabelAsync(), () => !this.IsBusy & this.SelectedLabel != null);
			this.LabelPreviewCommand = new DelegateCommand(() => _ = this.LabelPreviewAsync(), () => !this.IsBusy & this.SelectedLabel != null);

			//
			// Subscribe to the running state changed event to update the running
			// status of the UI.
			//
			_ = this.EventAggregator.GetEvent<RunningStateChangedEvent>().Subscribe((a) =>
			{
				this.IsRunning = a.IsRunning;

				if (a.IsError)
				{
					this.StatusText = a.ErrorMessage;
				}

			}, ThreadOption.UIThread);

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
			// Subscribe to the timer event.
			//
			_ = this.EventAggregator.GetEvent<TimerEvent>().Subscribe((a) =>
			{
				foreach (IStoredImage label in this.Labels)
				{
					label.Refresh();
				}
			}, ThreadOption.UIThread);
		}

		protected IEventAggregator EventAggregator { get; set; }
		public IImageCacheRepository ImageCacheRepository { get; set; }
		protected CancellationTokenSource TokenSource { get; set; }

		public ObservableCollection<Resolution> Resolutions { get; } = new ObservableCollection<Resolution>();
		public ObservableCollection<LabelRotation> Rotations { get; } = new ObservableCollection<LabelRotation>();
		public ObservableCollection<LabelUnit> LabelUnits { get; } = new ObservableCollection<LabelUnit>();
		public ObservableCollection<IStoredImage> Labels { get; } = new ObservableCollection<IStoredImage>();
		public ObservableCollection<IPAddress> IpAddresses { get; } = new ObservableCollection<IPAddress>();

		public DelegateCommand StartCommand { get; set; }
		public DelegateCommand StopCommand { get; set; }
		public DelegateCommand TestLabelCommand { get; set; }
		public DelegateCommand ClearLabelsCommand { get; set; }
		public DelegateCommand BrowseCommand { get; set; }
		public DelegateCommand DeleteLabelCommand { get; set; }
		public DelegateCommand LabelPreviewCommand { get; set; }

		private LabelRotation _selectedRotation = null;
		public LabelRotation SelectedRotation
		{
			get
			{
				return _selectedRotation;
			}
			set
			{
				this.SetProperty(ref _selectedRotation, value);
			}
		}

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
				if (_selectedLabelUnit != null && value != null)
				{
					this.LabelWidth = Math.Round((new Length(this.LabelWidth, _selectedLabelUnit.Unit)).ToUnit(value.Unit).Value, 2);
					this.LabelHeight = Math.Round((new Length(this.LabelHeight, _selectedLabelUnit.Unit)).ToUnit(value.Unit).Value, 2);
				}

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
					this.StatusText = $"Viewing label {this.Labels.IndexOf(this.SelectedLabel) + 1:#,###} of {this.Labels.Count:#,###}";
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

		private IPAddress _selectedIpAddress = null;
		public IPAddress SelectedIpAddress
		{
			get
			{
				return _selectedIpAddress;
			}
			set
			{
				this.SetProperty(ref _selectedIpAddress, value);
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

		public void RefreshCommands()
		{
			//
			// refresh the state of all of the command buttons.
			//
			this.StartCommand.RaiseCanExecuteChanged();
			this.StopCommand.RaiseCanExecuteChanged();
			this.TestLabelCommand.RaiseCanExecuteChanged();
			this.ClearLabelsCommand.RaiseCanExecuteChanged();
			this.BrowseCommand.RaiseCanExecuteChanged();
			this.DeleteLabelCommand.RaiseCanExecuteChanged();
			this.LabelPreviewCommand.RaiseCanExecuteChanged();
		}

		protected Task LoadResolutions()
		{
			try
			{
				this.Resolutions.Add(new Resolution() { Dpmm = 6 });
				this.Resolutions.Add(new Resolution() { Dpmm = 8 });
				this.Resolutions.Add(new Resolution() { Dpmm = 12 });
				this.Resolutions.Add(new Resolution() { Dpmm = 24 });
				this.SelectedResolution = this.Resolutions.ElementAt(1);
			}
			catch (Exception ex)
			{
				this.StatusText = $"Error: {ex.Message}";
			}

			return Task.CompletedTask;
		}

		protected Task LoadRotations()
		{
			try
			{
				this.Rotations.Add(new() { Label = "None", Value = 0 });
				this.Rotations.Add(new() { Label = "90˚ Clockwise", Value = 90 });
				this.Rotations.Add(new() { Label = "180˚ Clockwise", Value = 180 });
				this.Rotations.Add(new() { Label = "270˚ Clockwise", Value = 270 });
				this.SelectedRotation = this.Rotations.ElementAt(0);
			}
			catch (Exception ex)
			{
				this.StatusText = $"Error: {ex.Message}";
			}

			return Task.CompletedTask;
		}

		protected Task LoadIpAddresses()
		{
			try
			{
				IPHostEntry entry = Dns.GetHostEntry(Dns.GetHostName());
				IPAddress a = entry.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
				this.IpAddresses.Add(IPAddress.Any);
				this.IpAddresses.Add(IPAddress.Loopback);
				this.IpAddresses.Add(a);
			}
			catch (Exception ex)
			{
				this.StatusText = $"Error: {ex.Message}";
			}

			return Task.CompletedTask;
		}

		protected Task LoadLabelUnits()
		{
			try
			{
				this.LabelUnits.Add(new LabelUnit() { Unit = UnitsNet.Units.LengthUnit.Inch });
				this.LabelUnits.Add(new LabelUnit() { Unit = UnitsNet.Units.LengthUnit.Millimeter });
				this.LabelUnits.Add(new LabelUnit() { Unit = UnitsNet.Units.LengthUnit.Centimeter });
				this.SelectedLabelUnit = this.LabelUnits.ElementAt(0);
			}
			catch (Exception ex)
			{
				this.StatusText = $"Error: {ex.Message}";
			}

			return Task.CompletedTask;
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
					LabelConfiguration = new()
					{
						Dpmm = this.SelectedResolution.Dpmm,
						LabelHeight = this.LabelHeight,
						LabelWidth = this.LabelWidth,
						Unit = this.SelectedLabelUnit.Unit,
						LabelRotation = this.SelectedRotation
					},
					IpAddress = this.SelectedIpAddress,
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

		protected async Task StopAsync()
		{
			try
			{
				//
				// Send a NOP command.
				//
				IPAddress ip = this.SelectedIpAddress == IPAddress.Any ? IPAddress.Loopback : this.SelectedIpAddress;
				(bool result, string errorMessage) = await TestClient.SendStringAsync(ip, this.Port, "NOP");

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
		}

		protected async Task TestLabelAsync()
		{
			try
			{
				IPAddress ip = this.SelectedIpAddress == IPAddress.Any ? IPAddress.Loopback : this.SelectedIpAddress;
				(bool result, string errorMessage) = await TestClient.SendStringAsync(ip, this.Port, await TestLabel.GetZplAsync());

				if (!result)
				{
					this.StatusText = errorMessage;
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
				this.IsBusy = true;
				this.StatusText = "Loading cached labels...";
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
					await Task.Delay(1);
					this.Labels.Add(label);
				}

				//
				// Select the last label.
				//
				if (this.Labels.Any())
				{
					this.SelectedLabel = this.Labels.Last();
				}
				else
				{
					this.SelectedLabel = null;
				}
			}
			catch (Exception ex)
			{
				this.StatusText = $"Error: {ex.Message}";
			}
			finally
			{
				this.IsBusy = false;
				this.RefreshCommands();
			}
		}

		protected Task BrowseCommandAsync()
		{
			//
			// Open the image with the default system viewer.
			//
			Process.Start(new ProcessStartInfo(this.ImagePath) { UseShellExecute = true });
			return Task.CompletedTask;
		}

		protected async Task DeleteLabelAsync()
		{
			try
			{
				if (this.SelectedLabel != null)
				{
					int currentIndex = this.Labels.IndexOf(this.SelectedLabel);

					if (await this.ImageCacheRepository.DeleteImageAsync(this.ImagePath, Path.GetFileName(this.SelectedLabel.FullPath)))
					{
						this.Labels.Remove(this.SelectedLabel);

						if (this.Labels.Any())
						{
							IStoredImage label = this.Labels.ElementAt(currentIndex >= this.Labels.Count() ? currentIndex - 1 : currentIndex);

							if (label != null)
							{
								this.SelectedLabel = label;
							}
							else
							{
								this.SelectedLabel = this.Labels.Last();
							}
						}
						else
						{
							this.SelectedLabel = null;
						}
					}
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

		public Task LabelPreviewAsync(IStoredImage item = null)
		{
			if (this.SelectedLabel != null)
			{
				Process.Start(new ProcessStartInfo(item == null ? this.SelectedLabel.FullPath : item.FullPath) { UseShellExecute = true });
			}

			return Task.CompletedTask;
		}

		public string Version
		{
			get
			{
				Version version = Assembly.GetEntryAssembly().GetName().Version;
				return $"{version.Major}.{version.Minor}.{version.Build}";
			}
		}

		public string Title
		{
			get
			{
				return $"Virtual ZPL Printer v{this.Version}";
			}
		}
	}
}
