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
using System.Diagnostics;
using System.Net;
using System.Windows;
using Diamond.Core.Repository;
using Labelary.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using UnitsNet;
using UnitsNet.Units;
using VirtualPrinter.Db.Abstractions;
using VirtualPrinter.Db.Ef;
using VirtualPrinter.HostedService.PrintSystem;
using VirtualPrinter.Repository.HostAddresses;
using VirtualPrinter.Repository.LabelParameters;
using VirtualPrinter.Views;

namespace VirtualPrinter.ViewModels
{
	public class ConfigurationViewModel : BindableBase
	{
		public ConfigurationViewModel(IServiceProvider serviceProvider, IRepositoryFactory repositoryFactory, IPhysicalPrinterFactory physicalPrinterFactory)
		{
			this.ServiceProvider = serviceProvider;
			this.RepositoryFactory = repositoryFactory;
			this.PhysicalPrinterFactory = physicalPrinterFactory;

			this.BrowseCommand = new(async () => await this.BrowseCommandAsync(), () => this.SelectedPrinterConfiguration != null);
			this.UndoCommand = new(async () => await this.UndoCommandAsync(), () => this.Changes);
			this.CloseCommand = new(() => { }, () => !this.Changes);
			this.AddCommand = new(async () => await this.AddCommandAsync(), () => !this.Changes);
			this.DeleteCommand = new(async () => await this.DeleteCommandAsync(), () => !this.Changes && this.SelectedPrinterConfiguration != null && this.PrinterConfigurations.Count > 1);
			this.SaveCommand = new(async () => await this.SaveCommandAsync(), () => this.Changes);
			this.CloneCommand = new(async () => await this.CloneCommandAsync(), () => !this.Changes && this.SelectedPrinterConfiguration != null);
			this.FilterEditCommand = new(async () => await this.FilterEditCommandAsync(), () => this.SelectedPrinterConfiguration != null);
			this.PrinterEditCommand = new(async () => await this.PrinterEditCommandAsync(), () => this.SelectedPrinterConfiguration != null);
		}

		protected IServiceProvider ServiceProvider { get; set; }
		protected IRepositoryFactory RepositoryFactory { get; set; }
		protected IPhysicalPrinterFactory PhysicalPrinterFactory { get; set; }

		public DelegateCommand BrowseCommand { get; set; }
		public DelegateCommand UndoCommand { get; set; }
		public DelegateCommand CloseCommand { get; set; }
		public DelegateCommand AddCommand { get; set; }
		public DelegateCommand DeleteCommand { get; set; }
		public DelegateCommand SaveCommand { get; set; }
		public DelegateCommand CloneCommand { get; set; }
		public DelegateCommand FilterEditCommand { get; set; }
		public DelegateCommand PrinterEditCommand { get; set; }

		public ObservableCollection<PrinterConfigurationViewModel> PrinterConfigurations { get; } = [];
		public ObservableCollection<IHostAddress> HostAddresses { get; } = [];
		public ObservableCollection<IResolution> Resolutions { get; } = [];
		public ObservableCollection<ILabelRotation> Rotations { get; } = [];
		public ObservableCollection<ILabelUnit> LabelUnits { get; } = [];
		public ObservableCollection<FilterViewModel> Filters { get; } = [];

		private PrinterConfigurationViewModel _selectPrinterConfiguration = null;
		public PrinterConfigurationViewModel SelectedPrinterConfiguration
		{
			get
			{
				return this._selectPrinterConfiguration;
			}
			set
			{
				this.SetProperty(ref this._selectPrinterConfiguration, value);
				this.OnSelectedPrinterConfigurationChanged();
			}
		}

		private bool _changes = false;
		public bool Changes
		{
			get
			{
				return this._changes;
			}
			set
			{
				this.SetProperty(ref this._changes, value);
				this.RefreshCommands();
			}
		}

		private string _name = null;
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				this.SetProperty(ref this._name, value);
				this.Changes = true;
			}
		}

		private string _selectedHostAddress = null;
		public string SelectedHostAddress
		{
			get
			{
				return this._selectedHostAddress;
			}
			set
			{
				this.SetProperty(ref this._selectedHostAddress, value);
				this.Changes = true;
			}
		}

		private int _port = 0;
		public int Port
		{
			get
			{
				return this._port;
			}
			set
			{
				this.SetProperty(ref this._port, value);
				this.Changes = true;
			}
		}

		private ILabelUnit _selectedLabelUnit = null;
		public ILabelUnit SelectedLabelUnit
		{
			get
			{
				return this._selectedLabelUnit;
			}
			set
			{
				if (this._selectedLabelUnit != null && value != null)
				{
					this.LabelWidth = Math.Round(new Length(this.LabelWidth, this._selectedLabelUnit.Unit).ToUnit(value.Unit).Value, 2);
					this.LabelHeight = Math.Round(new Length(this.LabelHeight, this._selectedLabelUnit.Unit).ToUnit(value.Unit).Value, 2);
				}

				this.SetProperty(ref this._selectedLabelUnit, value);
				this.Changes = true;
			}
		}

		private double _labelWidth = 0;
		public double LabelWidth
		{
			get
			{
				return this._labelWidth;
			}
			set
			{
				this.SetProperty(ref this._labelWidth, value);
				this.Changes = true;
			}
		}

		private double _labelHeight = 0;
		public double LabelHeight
		{
			get
			{
				return this._labelHeight;
			}
			set
			{
				this.SetProperty(ref this._labelHeight, value);
				this.Changes = true;
			}
		}

		private IResolution _selectResolution = null;
		public IResolution SelectedResolution
		{
			get
			{
				return this._selectResolution;
			}
			set
			{
				this.SetProperty(ref this._selectResolution, value);
				this.Changes = true;
			}
		}

		private ILabelRotation _selectedRotation = null;
		public ILabelRotation SelectedRotation
		{
			get
			{
				return this._selectedRotation;
			}
			set
			{
				this.SetProperty(ref this._selectedRotation, value);
				this.Changes = true;
			}
		}

		private string _imagePath = null;
		public string ImagePath
		{
			get
			{
				return this._imagePath;
			}
			set
			{
				this.SetProperty(ref this._imagePath, value);
				this.Changes = true;
			}
		}

		private IPhysicalPrinter _physicalPrinter = null;
		public IPhysicalPrinter PhysicalPrinter
		{
			get
			{
				return this._physicalPrinter;
			}
			set
			{
				this.SetProperty(ref this._physicalPrinter, value);
				this.Changes = true;
			}
		}

		public string FilterDescription => this.Filters.Any() ? string.Join(" | ", this.Filters.OrderBy(t => t.Priority).Select(t => $"[{(t.TreatAsRegularExpression ? "(rgx)=>'" : "'")}{t.Find}' to '{t.Replace}']")).Limit(100) : "No Filters";

		public async Task InitializeAsync()
		{
			try
			{
				await this.LoadResolutionsAsync();
				await this.LoadRotationsAsync();
				await this.LoadHostAddresses();
				await this.LoadLabelUnitsAsync();
				await this.LoadPrinterConfigurationsAsync();
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

		protected async Task LoadPrinterConfigurationsAsync(int id = 0)
		{
			try
			{
				//
				// Clear the list.
				//
				this.PrinterConfigurations.Clear();

				//
				// Get the database context.
				//
				using (VirtualPrinterContext context = this.ServiceProvider.GetRequiredService<VirtualPrinterContext>())
				{
					context.File = FileLocations.Database.FullName;

					//
					// Get the repository for the printer configurations.
					//
					using IQueryableRepository<IPrinterConfiguration> repository = await this.RepositoryFactory.GetQueryableAsync<IPrinterConfiguration>();

					//
					// Load the printer configurations.
					//
					IEnumerable<PrinterConfigurationViewModel> items = [.. (from tbl in repository.GetQueryable(context)
																	orderby tbl.Id
																	select new PrinterConfigurationViewModel(this.PhysicalPrinterFactory ,tbl))];

					this.PrinterConfigurations.AddRange(items);

					//
					// Set the default configuration.
					//
					if (id == 0)
					{
						this.SelectedPrinterConfiguration = this.PrinterConfigurations.FirstOrDefault();
					}
					else
					{
						this.SelectedPrinterConfiguration = this.PrinterConfigurations.Where(t => t.Id == id).SingleOrDefault();
						this.SelectedPrinterConfiguration ??= this.PrinterConfigurations.FirstOrDefault();
					}
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

		protected async Task LoadResolutionsAsync()
		{
			try
			{
				//
				// Get the resolution repository.
				//
				IReadOnlyRepository<IResolution> repository = await this.RepositoryFactory.GetReadOnlyAsync<IResolution>();

				//
				// Get all resolutions.
				//
				IEnumerable<IResolution> resolutions = await repository.GetAllAsync();

				//
				// Load the resolutions into the observable collection.
				//
				foreach (IResolution resolution in resolutions)
				{
					this.Resolutions.Add(resolution);
				}

				this.SelectedResolution = this.Resolutions.ElementAt(1);
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

		protected async Task LoadRotationsAsync()
		{
			try
			{
				//
				// Get the resolution repository.
				//
				IReadOnlyRepository<ILabelRotation> repository = await this.RepositoryFactory.GetReadOnlyAsync<ILabelRotation>();

				//
				// Get all resolutions.
				//
				IEnumerable<ILabelRotation> rotations = await repository.GetAllAsync();

				//
				// Load the resolutions into the observable collection.
				//
				foreach (ILabelRotation rotation in rotations)
				{
					this.Rotations.Add(rotation);
				}

				this.SelectedRotation = this.Rotations.ElementAt(0);
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

		protected async Task LoadHostAddresses()
		{
			try
			{
				//
				// Get the repository.
				//
				IReadOnlyRepository<IHostAddress> repository = await this.RepositoryFactory.GetReadOnlyAsync<IHostAddress>();

				//
				// Get all items.
				//
				IEnumerable<IHostAddress> items = await repository.GetAllAsync();

				//
				// Load the items to the observable collection.
				//
				foreach (IHostAddress item in items)
				{
					this.HostAddresses.Add(item);
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

		protected async Task LoadLabelUnitsAsync()
		{
			try
			{
				//
				// Get the resolution repository.
				//
				IReadOnlyRepository<ILabelUnit> repository = await this.RepositoryFactory.GetReadOnlyAsync<ILabelUnit>();

				//
				// Get all resolutions.
				//
				IEnumerable<ILabelUnit> labelUnits = await repository.GetAllAsync();

				//
				// Load the resolutions into the observable collection.
				//
				foreach (ILabelUnit labelUnit in labelUnits)
				{
					this.LabelUnits.Add(labelUnit);
				}

				this.SelectedLabelUnit = this.LabelUnits.ElementAt(0);
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

		protected async void OnSelectedPrinterConfigurationChanged()
		{
			try
			{
				if (this.SelectedPrinterConfiguration != null)
				{
					this.Name = this.SelectedPrinterConfiguration.Name;
					this.SelectedHostAddress = this.SelectedPrinterConfiguration.HostAddress;
					this.Port = this.SelectedPrinterConfiguration.Port;
					this.SelectedLabelUnit = this.LabelUnits.Where(t => t.Unit == (LengthUnit)this.SelectedPrinterConfiguration.LabelUnit).SingleOrDefault();
					this.LabelWidth = this.SelectedPrinterConfiguration.LabelWidth;
					this.LabelHeight = this.SelectedPrinterConfiguration.LabelHeight;
					this.SelectedResolution = this.Resolutions.Where(t => t.Dpmm == this.SelectedPrinterConfiguration.ResolutionInDpmm).SingleOrDefault();
					this.SelectedRotation = this.Rotations.Where(t => t.Value == this.SelectedPrinterConfiguration.RotationAngle).SingleOrDefault();
					this.ImagePath = this.SelectedPrinterConfiguration.ImagePath;
					this.PhysicalPrinter = await this.PhysicalPrinterFactory.DeserializeAsync(this.SelectedPrinterConfiguration.PhysicalPrinter);
				}
				else
				{
					this.Name = null;
					this.SelectedHostAddress = null;
					this.Port = 0;
					this.LabelWidth = 0;
					this.LabelHeight = 0;
					this.SelectedLabelUnit = null;
					this.SelectedResolution = null;
					this.SelectedRotation = null;
					this.ImagePath = null;
					this.PhysicalPrinter = null;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Properties.Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.LoadFilters();
				this.Changes = false;
				this.RefreshCommands();
			}
		}

		protected void LoadFilters()
		{
			try
			{
				//
				// Load the ZPL filters.
				//
				this.Filters.Clear();

				if (this.SelectedPrinterConfiguration != null)
				{
					IList<FilterViewModel> filters = FilterViewModel.ToList(this.SelectedPrinterConfiguration.Filters);
					this.Filters.AddRange(filters);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Properties.Strings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RaisePropertyChanged(nameof(this.FilterDescription));
				this.RefreshCommands();
			}
		}

		public void RefreshCommands()
		{
			//
			// Refresh the state of all of the command buttons.
			//
			this.UndoCommand.RaiseCanExecuteChanged();
			this.CloseCommand.RaiseCanExecuteChanged();
			this.AddCommand.RaiseCanExecuteChanged();
			this.SaveCommand.RaiseCanExecuteChanged();
			this.DeleteCommand.RaiseCanExecuteChanged();
			this.BrowseCommand.RaiseCanExecuteChanged();
			this.CloneCommand.RaiseCanExecuteChanged();
			this.FilterEditCommand.RaiseCanExecuteChanged();
			this.PrinterEditCommand.RaiseCanExecuteChanged();
		}

		protected async Task UndoCommandAsync()
		{
			try
			{
				if (this.SelectedPrinterConfiguration != null)
				{
					if (this.SelectedPrinterConfiguration.Id == 0)
					{
						this.PrinterConfigurations.Remove(this.SelectedPrinterConfiguration);
						this.SelectedPrinterConfiguration = this.PrinterConfigurations.Last();
					}
					else
					{
						await this.LoadPrinterConfigurationsAsync(this.SelectedPrinterConfiguration.Id);
						this.OnSelectedPrinterConfigurationChanged();
					}
				}
				else
				{
					this.OnSelectedPrinterConfigurationChanged();
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

		protected async Task AddCommandAsync()
		{
			try
			{
				//
				// Get a writable repository.
				//
				using IWritableRepository<IPrinterConfiguration> repository = await this.RepositoryFactory.GetWritableAsync<IPrinterConfiguration>();

				IPrinterConfiguration item = await repository.ModelFactory.CreateAsync();
				item.Name = this.GetNewName(Properties.Strings.New_Printer_Configuratio_Name);
				item.HostAddress = IPAddress.Loopback.ToString();
				item.Port = 9100;
				item.LabelHeight = 6;
				item.LabelWidth = 4;
				item.LabelUnit = (int)LengthUnit.Inch;
				item.ResolutionInDpmm = 8;
				item.RotationAngle = 0;
				item.ImagePath = FileLocations.ImageCache.FullName;

				PrinterConfigurationViewModel viewModelItem = new(this.PhysicalPrinterFactory, item);
				this.PrinterConfigurations.Add(viewModelItem);
				this.SelectedPrinterConfiguration = viewModelItem;
				this.Changes = true;
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

		protected async Task SaveCommandAsync()
		{
			try
			{
				if (this.SelectedPrinterConfiguration != null)
				{
					//
					// Get the database context.
					//
					using (VirtualPrinterContext context = this.ServiceProvider.GetRequiredService<VirtualPrinterContext>())
					{
						context.File = FileLocations.Database.FullName;

						//
						// Get a writable repository.
						//
						using IWritableRepository<IPrinterConfiguration> repository = await this.RepositoryFactory.GetWritableAsync<IPrinterConfiguration>();

						//
						// Map the data.
						//
						this.SelectedPrinterConfiguration.Name = this.Name;
						this.SelectedPrinterConfiguration.HostAddress = this.SelectedHostAddress;
						this.SelectedPrinterConfiguration.Port = this.Port;
						this.SelectedPrinterConfiguration.LabelUnit = (int)this.SelectedLabelUnit.Unit;
						this.SelectedPrinterConfiguration.LabelHeight = this.LabelHeight;
						this.SelectedPrinterConfiguration.LabelWidth = this.LabelWidth;
						this.SelectedPrinterConfiguration.ResolutionInDpmm = this.SelectedResolution.Dpmm;
						this.SelectedPrinterConfiguration.RotationAngle = this.SelectedRotation.Value;
						this.SelectedPrinterConfiguration.ImagePath = this.ImagePath;
						this.SelectedPrinterConfiguration.Filters = JsonConvert.SerializeObject(this.Filters.ToArray());
						this.SelectedPrinterConfiguration.PhysicalPrinter = JsonConvert.SerializeObject(this.PhysicalPrinter);

						//
						// Track the current item.
						//
						int id = this.SelectedPrinterConfiguration.Id;

						//
						// Check if this is an add or update.
						//
						if (this.SelectedPrinterConfiguration.Id == 0)
						{
							(int result, IPrinterConfiguration newItem) = await repository.AddAsync(context, this.SelectedPrinterConfiguration.Item);
							id = newItem.Id;
						}
						else
						{
							int result = await repository.UpdateAsync(context, this.SelectedPrinterConfiguration.Item);
						}

						//
						// Reload the list and select this object.
						//
						await this.LoadPrinterConfigurationsAsync(id);
					}
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

		protected async Task DeleteCommandAsync()
		{
			try
			{
				if (this.SelectedPrinterConfiguration != null && this.PrinterConfigurations.Count > 1)
				{
					string message = string.Format(Properties.Strings.MessageBox_DeletePrinterConfiguration_Message, this.SelectedPrinterConfiguration.Name);
					if (MessageBox.Show(message, Properties.Strings.MessageBox_DeletePrinterConfiguration_Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
					{
						//
						// Get the database context.
						//
						using (VirtualPrinterContext context = this.ServiceProvider.GetRequiredService<VirtualPrinterContext>())
						{
							context.File = FileLocations.Database.FullName;

							//
							// Get a writable repository.
							//
							using IWritableRepository<IPrinterConfiguration> repository = await this.RepositoryFactory.GetWritableAsync<IPrinterConfiguration>();
							int affected = await repository.DeleteAsync(context, this.SelectedPrinterConfiguration.Item);

							if (affected > 0)
							{
								await this.LoadPrinterConfigurationsAsync();
							}
							else
							{
								MessageBox.Show(Properties.Strings.MessageBox_DeletePrinterConfigurationFailed_Message, Properties.Strings.MessageBox_DeletePrinterConfigurationFailed_Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
							}
						}
					}
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

		protected Task BrowseCommandAsync()
		{
			//
			// Open the image with the default system viewer.
			//
			Process.Start(new ProcessStartInfo(this.SelectedPrinterConfiguration.ImagePath) { UseShellExecute = true });
			return Task.CompletedTask;
		}

		protected async Task CloneCommandAsync()
		{
			try
			{
				if (this.SelectedPrinterConfiguration != null)
				{
					//
					// Get a writable repository.
					//
					using IWritableRepository<IPrinterConfiguration> repository = await this.RepositoryFactory.GetWritableAsync<IPrinterConfiguration>();

					IPrinterConfiguration item = await repository.ModelFactory.CreateAsync();
					item.Name = this.GetNewName($"{this.SelectedPrinterConfiguration.Name}-Copy");
					item.HostAddress = this.SelectedPrinterConfiguration.HostAddress;
					item.Port = this.SelectedPrinterConfiguration.Port;
					item.LabelHeight = this.SelectedPrinterConfiguration.LabelHeight;
					item.LabelWidth = this.SelectedPrinterConfiguration.LabelWidth;
					item.LabelUnit = this.SelectedPrinterConfiguration.LabelUnit;
					item.ResolutionInDpmm = this.SelectedPrinterConfiguration.ResolutionInDpmm;
					item.RotationAngle = this.SelectedPrinterConfiguration.RotationAngle;
					item.ImagePath = this.SelectedPrinterConfiguration.ImagePath;
					item.Filters = this.SelectedPrinterConfiguration?.Filters;
					item.PhysicalPrinter = this.SelectedPrinterConfiguration?.PhysicalPrinter;

					PrinterConfigurationViewModel viewModelItem = new(this.PhysicalPrinterFactory, item);
					this.PrinterConfigurations.Add(viewModelItem);
					this.SelectedPrinterConfiguration = viewModelItem;
					this.Changes = true;
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

		protected Task FilterEditCommandAsync()
		{
			try
			{
				//
				// Get the view.
				//
				EditFiltersView view = this.ServiceProvider.GetService<EditFiltersView>();
				view.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				view.ViewModel.PrinterConfiguration = this.SelectedPrinterConfiguration;

				//
				// Show the dialog.
				//
				_ = view.ShowDialog();

				if (view.ViewModel.Updated)
				{
					//
					// Load the changed filters.
					//
					this.LoadFilters();

					this.Changes = true;
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

			return Task.CompletedTask;
		}

		protected Task PrinterEditCommandAsync()
		{
			try
			{
				//
				// Get the view.
				//
				EditPrinterView view = this.ServiceProvider.GetService<EditPrinterView>();
				view.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				view.ViewModel.PhysicalPrinter = this.PhysicalPrinter;

				//
				// Show the dialog.
				//
				_ = view.ShowDialog();

				if (view.ViewModel.Updated)
				{
					this.PhysicalPrinter = view.ViewModel.PhysicalPrinter;
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

			return Task.CompletedTask;
		}

		protected string GetNewName(string baseName)
		{
			string returnValue = baseName;
			int i = 1;

			do
			{
				returnValue = $"{baseName} {i}";
				i++;
			} while (this.PrinterConfigurations.Where(t => t.Name == returnValue).Any());

			return returnValue;
		}
	}
}
