﻿/*
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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using Diamond.Core.Repository;
using ImageCache.Abstractions;
using Labelary.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using UnitsNet.Units;
using VirtualPrinter.ApplicationSettings;
using VirtualPrinter.Db.Abstractions;
using VirtualPrinter.Db.Ef;
using VirtualPrinter.HostedService.PrintSystem;
using VirtualPrinter.PublishSubscribe;
using VirtualPrinter.Views;

namespace VirtualPrinter.ViewModels
{
	public class MainViewModel : BindableBase
	{
		public MainViewModel(IEventAggregator eventAggregator, ISettings settings, IServiceProvider serviceProvider, IImageCacheRepository imageCacheRepository, IRepositoryFactory repositoryFactory, IPhysicalPrinterFactory physicalPrinterFactory)
		{
			this.EventAggregator = eventAggregator;
			this.Settings = settings;
			this.ServiceProvider = serviceProvider;
			this.ImageCacheRepository = imageCacheRepository;
			this.RepositoryFactory = repositoryFactory;
			this.PhysicalPrinterFactory = physicalPrinterFactory;

			this.StartCommand = new(() => _ = this.StartAsync(), () => !this.IsBusy && !this.IsRunning && this.SelectedPrinterConfiguration != null);
			this.StopCommand = new(() => _ = this.StopAsync(), () => !this.IsBusy && this.IsRunning);
			this.SendTestLabelCommand = new(() => _ = this.SendTestLabelAsync(), () => !this.IsBusy && this.IsRunning);
			this.ClearLabelsCommand = new(() => _ = this.ClearLabelsAsync(), () => !this.IsBusy && this.Labels.Count > 0);
			this.DeleteLabelCommand = new(() => _ = this.DeleteLabelAsync(), () => !this.IsBusy && this.SelectedLabel != null);
			this.LabelPreviewCommand = new(() => _ = this.LabelPreviewAsync(), () => !this.IsBusy && this.SelectedLabel != null);
			this.LabelWarningsCommand = new(() => _ = this.LabelWarningAsync(), () => this.OnLabelWarningAsync());
			this.EditCommand = new(() => _ = this.EditPrinterConfigurationAsync(), () => !this.IsBusy && !this.IsRunning);
			this.GlobalSettingsCommand = new(() => _ = this.GlobalSettingsAsync(), () => !this.IsBusy && !this.IsRunning);
			this.AboutCommand = new(() => _ = this.AboutAsync(), () => !this.IsBusy && !this.IsRunning);
			this.TestLabelaryCommand = new(() => _ = this.TestLabelaryAsync(), () => !this.IsBusy && !this.IsRunning);
			this.FontManagerCommand = new(() => _ = this.FontManagerAsync(), () => !this.IsBusy && !this.IsRunning);

			//
			// Load the printer configurations.
			//
			_ = this.LoadPrinterConfigurations();

			//
			// Subscribe to the running state changed event to update the running
			// status of the UI.
			//
			_ = this.EventAggregator.GetEvent<RunningStateChangedEvent>().Subscribe((e) =>
			{
				this.IsRunning = e.IsRunning;

				if (e.IsError)
				{
					this.StatusText = e.ErrorMessage;
				}

			}, ThreadOption.UIThread);

			//
			// Subscribe to the label created event to add all new labels
			// to the UI.
			//
			_ = this.EventAggregator.GetEvent<LabelCreatedEvent>().Subscribe((e) =>
			  {
				  //
				  // Add the new label to the collection.
				  //
				  StoredImageViewModel viewModel = new(this.EventAggregator, e.Label);
				  this.Labels.Add(viewModel);
				  this.RaisePropertyChanged(nameof(this.Labels));

				  //
				  // Make the new label the currently selected label.
				  //
				  this.SelectedLabel = viewModel;
			  }, ThreadOption.UIThread);

			//
			// Subscribe to the timer event.
			//
			_ = this.EventAggregator.GetEvent<TimerEvent>().Subscribe((e) =>
			{
				foreach (StoredImageViewModel label in this.Labels)
				{
					label.StoredImage.Refresh();
				}
			}, ThreadOption.UIThread);

			//
			// Subscribe to the window hidden event.
			//
			_ = this.EventAggregator.GetEvent<WindowHiddenEvent>().Subscribe((e) =>
			{
				this.SendTestView = null;
				this.RefreshCommands();
			}, ThreadOption.UIThread);

			//
			// Subscribe to the view meta data event event.
			//
			_ = this.EventAggregator.GetEvent<ViewMetaDataEvent<StoredImageViewModel>>().Subscribe((e) =>
			{
				this.SelectedLabel = e.Item;

				if (e.Action == ViewMetaDataArgs<StoredImageViewModel>.ActionType.Warnings)
				{
					this.LabelWarningAsync();
				}
				else if (e.Action == ViewMetaDataArgs<StoredImageViewModel>.ActionType.Image)
				{
					this.LabelPreviewAsync();
				}

			}, ThreadOption.UIThread);
		}

		protected IEventAggregator EventAggregator { get; set; }
		protected ISettings Settings { get; set; }
		protected IServiceProvider ServiceProvider { get; set; }
		public IImageCacheRepository ImageCacheRepository { get; set; }
		protected IRepositoryFactory RepositoryFactory { get; set; }
		protected IPhysicalPrinterFactory PhysicalPrinterFactory { get; set; }

		public SendTestView SendTestView { get; set; }
		protected CancellationTokenSource TokenSource { get; set; }

		public ObservableCollection<StoredImageViewModel> Labels { get; } = [];
		public ObservableCollection<PrinterConfigurationViewModel> PrinterConfigurations { get; } = [];

		public DelegateCommand StartCommand { get; set; }
		public DelegateCommand StopCommand { get; set; }
		public DelegateCommand SendTestLabelCommand { get; set; }
		public DelegateCommand ClearLabelsCommand { get; set; }
		public DelegateCommand DeleteLabelCommand { get; set; }
		public DelegateCommand LabelPreviewCommand { get; set; }
		public DelegateCommand LabelWarningsCommand { get; set; }
		public DelegateCommand EditCommand { get; set; }
		public DelegateCommand AboutCommand { get; set; }
		public DelegateCommand GlobalSettingsCommand { get; set; }
		public DelegateCommand TestLabelaryCommand { get; set; }
		public DelegateCommand FontManagerCommand { get; set; }

		private PrinterConfigurationViewModel _printerConfiguration = null;
		public PrinterConfigurationViewModel SelectedPrinterConfiguration
		{
			get
			{
				return this._printerConfiguration;
			}
			set
			{
				bool pathChanged = this._printerConfiguration?.ImagePath != value?.ImagePath;

				this.SetProperty(ref this._printerConfiguration, value);
				this.RefreshCommands();

				if (pathChanged)
				{
					_ = this.LoadLabelsAsync();
				}
			}
		}

		private StoredImageViewModel _selectedLabel = null;
		public StoredImageViewModel SelectedLabel
		{
			get
			{
				return this._selectedLabel;
			}
			set
			{
				this.SetProperty(ref this._selectedLabel, value);

				if (this.SelectedLabel != null)
				{
					this.StatusText = $"{Properties.Strings.Main_Status_ViewingLabel} {this.Labels.IndexOf(this.SelectedLabel) + 1:#,###} {Properties.Strings.Main_Status_Of} {this.Labels.Count:#,###}";
				}
				else
				{
					if (this.Labels.Any())
					{
						if (this.Labels.Count == 1)
						{
							this.StatusText = $"{this.Labels.Count} {Properties.Strings.Main_Status_Label}";
						}
						else
						{
							this.StatusText = $"{this.Labels.Count} {Properties.Strings.Main_Status_Labels}";
						}
					}
					else
					{
						this.StatusText = Properties.Strings.Main_Status_Ready;
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
				return this._isBusy;
			}
			set
			{
				this.SetProperty(ref this._isBusy, value);
				this.RefreshCommands();
			}
		}

		private bool _isRunning = false;
		public bool IsRunning
		{
			get
			{
				return this._isRunning;
			}
			set
			{
				this.SetProperty(ref this._isRunning, value);
				this.RefreshCommands();
			}
		}

		private string _statusText = null;
		public string StatusText
		{
			get
			{
				return this._statusText;
			}
			set
			{
				this.SetProperty(ref this._statusText, value);
			}
		}

		private bool _autoStart = false;
		public bool AutoStart
		{
			get
			{
				return this._autoStart;
			}
			set
			{
				this.SetProperty(ref this._autoStart, value);
			}
		}

		public async Task InitializeAsync()
		{
			try
			{
				//
				// Auto start
				//
				if (this.AutoStart && this.SelectedPrinterConfiguration != null)
				{
					await Task.Delay(250);
					_ = this.StartAsync();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Properties.Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}
		}

		protected async Task LoadPrinterConfigurations()
		{
			try
			{
				//
				// Clear the list.
				//
				this.PrinterConfigurations.Clear();

				//
				// Get the repository for the printer configurations.
				//
				using IReadOnlyRepository<IPrinterConfiguration> repository = await this.RepositoryFactory.GetReadOnlyAsync<IPrinterConfiguration>();

				//
				// Get the database context.
				//
				using (VirtualPrinterContext context = this.ServiceProvider.GetRequiredService<VirtualPrinterContext>())
				{
					context.File = FileLocations.Database.FullName;

					//
					// Ensure the database exists.
					//
					DirectoryInfo dir = new(FileLocations.Database.DirectoryName);
					dir.Create();

					//
					// Create the database if it does not exist.
					//
					await context.Database.EnsureCreatedAsync();
					await context.CheckUpgradeAsync();

					//
					// Load the printer configurations.
					//
					IQueryable<PrinterConfigurationViewModel> items = from tbl in repository.AsQueryable().GetQueryable(context)
																	  orderby tbl.Id
																	  select new PrinterConfigurationViewModel(this.PhysicalPrinterFactory, tbl);

					this.PrinterConfigurations.AddRange(items);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Properties.Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}
		}

		public async void RefreshCommands()
		{
			//
			// Refresh the state of all of the command buttons.
			//
			this.StartCommand.RaiseCanExecuteChanged();
			this.StopCommand.RaiseCanExecuteChanged();
			this.SendTestLabelCommand.RaiseCanExecuteChanged();
			this.ClearLabelsCommand.RaiseCanExecuteChanged();
			this.DeleteLabelCommand.RaiseCanExecuteChanged();
			this.LabelPreviewCommand.RaiseCanExecuteChanged();
			this.LabelWarningsCommand.RaiseCanExecuteChanged();
			this.EditCommand.RaiseCanExecuteChanged();
			this.AboutCommand.RaiseCanExecuteChanged();
			this.GlobalSettingsCommand.RaiseCanExecuteChanged();
			this.TestLabelaryCommand.RaiseCanExecuteChanged();
			this.FontManagerCommand.RaiseCanExecuteChanged();

			await Task.Delay(1);
		}

		protected Task StartAsync()
		{
			try
			{
				this.StatusText = Properties.Strings.Main_Status_Ready;

				//
				// Get the filters.
				//
				IEnumerable<FilterViewModel> filters = FilterViewModel.ToList(this.SelectedPrinterConfiguration.Filters);

				//
				// Publish an event to start the listener. The arguments of the message
				// specify the label size, DPMM of the label, TCP port and the folder
				// location to use for label storage.
				//
				this.EventAggregator.GetEvent<StartEvent>().Publish(new StartEventArgs()
				{
					PrinterConfiguration = this.SelectedPrinterConfiguration.Item,
					LabelConfiguration = new()
					{
						Dpmm = this.SelectedPrinterConfiguration.ResolutionInDpmm,
						LabelHeight = this.SelectedPrinterConfiguration.LabelHeight,
						LabelWidth = this.SelectedPrinterConfiguration.LabelWidth,
						Unit = (LengthUnit)this.SelectedPrinterConfiguration.LabelUnit,
						LabelRotation = this.SelectedPrinterConfiguration.RotationAngle,
						LabelFilters = (from tbl in filters
										select new LabelFilter()
										{
											Priority = tbl.Priority,
											Find = tbl.Find,
											Replace = tbl.Replace,
											TreatAsRegularExpression = tbl.TreatAsRegularExpression
										}).ToArray()
					},
					IpAddress = IPAddress.Parse(this.SelectedPrinterConfiguration.HostAddress),
					Port = this.SelectedPrinterConfiguration.Port,
					ImagePath = this.SelectedPrinterConfiguration.ImagePath
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Properties.Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
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
				MessageBox.Show(ex.Message, Properties.Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}

			return Task.CompletedTask;
		}

		protected Task SendTestLabelAsync()
		{
			try
			{
				if (this.SendTestView == null)
				{
					this.SendTestView = this.ServiceProvider.GetService<SendTestView>();
					this.SendTestView.ViewModel.SelectedPrinterConfiguration = this.SelectedPrinterConfiguration;
					this.SendTestView.Show();
				}
				else
				{
					this.SendTestView.Activate();
				}
			}
			catch (Exception ex)
			{
				this.StatusText = $"{Properties.Strings.Error}: {ex.Message}";
			}
			finally
			{
				this.RefreshCommands();
			}

			return Task.CompletedTask;
		}

		protected async Task ClearLabelsAsync()
		{
			try
			{
				//
				// Clear the collection.
				//
				this.Labels.Clear();
				this.RaisePropertyChanged(nameof(this.Labels));

				//
				// Set the selected label to null.
				//
				this.SelectedLabel = null;

				//
				// Remove the labels from storage.
				//
				_ = await this.ImageCacheRepository.ClearAllAsync(this.SelectedPrinterConfiguration.ImagePath);
			}
			catch (Exception ex)
			{
				this.StatusText = $"{Properties.Strings.Error}: {ex.Message}";
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
				// Clear the current list.
				//
				this.IsBusy = true;
				this.StatusText = Properties.Strings.Main_Status_LoadingLabels;
				this.Labels.Clear();

				if (this.SelectedPrinterConfiguration != null)
				{
					//
					// Load the previous labels
					//
					IEnumerable<IStoredImage> labels = await this.ImageCacheRepository.GetAllAsync(this.SelectedPrinterConfiguration.ImagePath);

					//
					// Add the labels to the collection.
					//
					foreach (IStoredImage label in labels)
					{
						this.Labels.Add(new StoredImageViewModel(this.EventAggregator, label));
						await Task.Delay(1);
						this.RaisePropertyChanged(nameof(this.Labels));
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
			}
			catch (Exception ex)
			{
				this.StatusText = $"{Properties.Strings.Error}: {ex.Message}";
			}
			finally
			{
				this.IsBusy = false;
				this.RaisePropertyChanged(nameof(this.Labels));
			}
		}

		protected async Task DeleteLabelAsync()
		{
			try
			{
				if (this.SelectedLabel != null)
				{
					int currentIndex = this.Labels.IndexOf(this.SelectedLabel);

					if (await this.ImageCacheRepository.DeleteImageAsync(this.SelectedPrinterConfiguration.ImagePath, Path.GetFileName(this.SelectedLabel.StoredImage.FullPath)))
					{
						this.Labels.Remove(this.SelectedLabel);
						this.RaisePropertyChanged(nameof(this.Labels));

						if (this.Labels.Any())
						{
							StoredImageViewModel label = this.Labels.ElementAt(currentIndex >= this.Labels.Count ? currentIndex - 1 : currentIndex);

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
				this.StatusText = $"{Properties.Strings.Error}: {ex.Message}";
			}
			finally
			{
				this.RefreshCommands();
			}
		}

		public Task LabelPreviewAsync(StoredImageViewModel item = null)
		{
			try
			{
				item ??= this.SelectedLabel;

				if (item != null)
				{
					Process.Start(new ProcessStartInfo(item.StoredImage.FullPath) { UseShellExecute = true });
				}
			}
			catch (Exception ex)
			{
				this.StatusText = $"{Properties.Strings.Error}: {ex.Message}";
			}
			finally
			{
				this.RefreshCommands();
			}

			return Task.CompletedTask;
		}

		public bool OnLabelWarningAsync()
		{
			bool returnValue = false;

			if (!this.IsBusy && this.SelectedLabel != null)
			{
				returnValue = File.Exists(this.SelectedLabel.StoredImage.MetaDataFile);
			}

			return returnValue;
		}

		public Task LabelWarningAsync()
		{
			try
			{
				if (this.SelectedLabel != null)
				{
					ZplView view = this.ServiceProvider.GetService<ZplView>();

					string json = File.ReadAllText(this.SelectedLabel.StoredImage.MetaDataFile);
					view.ViewModel.LabelResponse = JsonConvert.DeserializeObject<GetLabelResponse>(json);

					view.ShowDialog();
				}
			}
			catch (Exception ex)
			{
				this.StatusText = $"{Properties.Strings.Error}: {ex.Message}";
			}
			finally
			{
				this.RefreshCommands();
			}

			return Task.CompletedTask;
		}

		public async Task EditPrinterConfigurationAsync()
		{
			try
			{
				//
				// Get the selected item.
				//
				int id = this.SelectedPrinterConfiguration.Id;

				//
				// Get the view.
				//
				ConfigurationView view = this.ServiceProvider.GetService<ConfigurationView>();
				view.WindowStartupLocation = WindowStartupLocation.CenterOwner;

				//
				// Show the dialog.
				//
				view.ShowDialog();

				//
				// Reload the printer configurations.
				//
				await this.LoadPrinterConfigurations();

				//
				// Reselect the item.
				//
				this.SelectedPrinterConfiguration = this.PrinterConfigurations.Where(t => t.Id == id).SingleOrDefault();
				this.SelectedPrinterConfiguration ??= this.PrinterConfigurations.FirstOrDefault();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Properties.Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}
		}

		public Task AboutAsync()
		{
			try
			{
				AboutView view = this.ServiceProvider.GetService<AboutView>();
				view.ShowDialog();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Properties.Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}

			return Task.CompletedTask;
		}

		public Task GlobalSettingsAsync()
		{
			try
			{
				GlobalSettingsView view = this.ServiceProvider.GetService<GlobalSettingsView>();
				view.ShowDialog();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Properties.Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}

			return Task.CompletedTask;
		}

		public Task TestLabelaryAsync()
		{
			try
			{
				TestLabelaryView view = this.ServiceProvider.GetService<TestLabelaryView>();
				view.ShowDialog();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Properties.Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}

			return Task.CompletedTask;
		}

		public Task FontManagerAsync()
		{
			try
			{
				FontManagerView view = this.ServiceProvider.GetService<FontManagerView>();
				view.ShowDialog();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Properties.Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}

			return Task.CompletedTask;
		}
	}
}
