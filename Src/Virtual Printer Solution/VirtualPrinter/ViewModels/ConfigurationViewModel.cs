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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
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
using VirtualPrinter.Models;
using VirtualPrinter.Views;

namespace VirtualPrinter.ViewModels
{
    public class ConfigurationViewModel : BindableBase
	{
		public ConfigurationViewModel(IServiceProvider serviceProvider, IRepositoryFactory repositoryFactory)
		{
			this.ServiceProvider = serviceProvider;
			this.RepositoryFactory = repositoryFactory;

			this.BrowseCommand = new DelegateCommand(async () => await this.BrowseCommandAsync(), () => this.SelectedPrinterConfiguration != null);
			this.UndoCommand = new DelegateCommand(async () => await this.UndoCommandAsync(), () => this.Changes);
			this.CloseCommand = new DelegateCommand(() => { }, () => !this.Changes);
			this.AddCommand = new DelegateCommand(async () => await this.AddCommandAsync(), () => !this.Changes);
			this.DeleteCommand = new DelegateCommand(async () => await this.DeleteCommandAsync(), () => !this.Changes && this.SelectedPrinterConfiguration != null && this.PrinterConfigurations.Count() > 1);
			this.SaveCommand = new DelegateCommand(async () => await this.SaveCommandAsync(), () => this.Changes);
			this.CloneCommand = new DelegateCommand(async () => await this.CloneCommandAsync(), () => !this.Changes && this.SelectedPrinterConfiguration != null);
			this.FilterEditCommand = new DelegateCommand(async () => await this.FilterEditCommandAsync(), () => this.SelectedPrinterConfiguration != null);
		}

		protected IServiceProvider ServiceProvider { get; set; }
		protected IRepositoryFactory RepositoryFactory { get; set; }

		public DelegateCommand BrowseCommand { get; set; }
		public DelegateCommand UndoCommand { get; set; }
		public DelegateCommand CloseCommand { get; set; }
		public DelegateCommand AddCommand { get; set; }
		public DelegateCommand DeleteCommand { get; set; }
		public DelegateCommand SaveCommand { get; set; }
		public DelegateCommand CloneCommand { get; set; }
		public DelegateCommand FilterEditCommand { get; set; }

		public ObservableCollection<IPAddress> IpAddresses { get; } = new ObservableCollection<IPAddress>();
		public ObservableCollection<Resolution> Resolutions { get; } = new ObservableCollection<Resolution>();
		public ObservableCollection<LabelRotation> Rotations { get; } = new ObservableCollection<LabelRotation>();
		public ObservableCollection<LabelUnit> LabelUnits { get; } = new ObservableCollection<LabelUnit>();
		public ObservableCollection<IPrinterConfiguration> PrinterConfigurations { get; } = new ObservableCollection<IPrinterConfiguration>();
		public ObservableCollection<FilterViewModel> Filters { get; } = new ObservableCollection<FilterViewModel>();

		private IPrinterConfiguration _selectPrinterConfiguration = null;
		public IPrinterConfiguration SelectedPrinterConfiguration
		{
			get
			{
				return _selectPrinterConfiguration;
			}
			set
			{
				this.SetProperty(ref _selectPrinterConfiguration, value);
				this.OnSelectedPrinterConfigurationChanged();
			}
		}

		private bool _changes = false;
		public bool Changes
		{
			get
			{
				return _changes;
			}
			set
			{
				this.SetProperty(ref _changes, value);
				this.RefreshCommands();
			}
		}

		private string _name = null;
		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				this.SetProperty(ref _name, value);
				this.Changes = true;
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
				this.Changes = true;
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
				this.Changes = true;
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
				this.Changes = true;
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
				this.Changes = true;
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
				this.Changes = true;
			}
		}

		private Resolution _selectResolution = null;
		public Resolution SelectedResolution
		{
			get
			{
				return _selectResolution;
			}
			set
			{
				this.SetProperty(ref _selectResolution, value);
				this.Changes = true;
			}
		}

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
				this.Changes = true;
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
				this.Changes = true;
			}
		}

		public string FilterDescription => this.Filters.Any() ? string.Join(" | ", this.Filters.OrderBy(t => t.Priority).Select(t => $"[{(t.TreatAsRegularExpression ? "(rgx)=>'" : "'")}{t.Find}' to '{t.Replace}']")).Limit(100) : "No Filters";

		public async Task InitializeAsync()
		{
			await this.LoadResolutionsAsync();
			await this.LoadRotationsAsync();
			await this.LoadIpAddressesAsync();
			await this.LoadLabelUnitsAsync();
			await this.LoadPrinterConfigurationsAsync();
		}

		protected async Task LoadPrinterConfigurationsAsync(int id = 0)
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
				IEnumerable<IPrinterConfiguration> items = (await repository.GetQueryableAsync(context)).OrderBy(t => t.Id).ToArray();
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

					if (this.SelectedPrinterConfiguration == null)
					{
						this.SelectedPrinterConfiguration = this.PrinterConfigurations.FirstOrDefault();
					}
				}
			}
		}

		protected Task LoadResolutionsAsync()
		{
			try
			{
				this.Resolutions.Add(new Resolution() { Dpmm = 6 });
				this.Resolutions.Add(new Resolution() { Dpmm = 8 });
				this.Resolutions.Add(new Resolution() { Dpmm = 12 });
				this.Resolutions.Add(new Resolution() { Dpmm = 24 });
				this.SelectedResolution = this.Resolutions.ElementAt(1);
			}
			catch
			{

			}

			return Task.CompletedTask;
		}

		protected Task LoadRotationsAsync()
		{
			try
			{
				this.Rotations.Add(new() { Label = "None", Value = 0 });
				this.Rotations.Add(new() { Label = "90˚ Clockwise", Value = 90 });
				this.Rotations.Add(new() { Label = "180˚ Clockwise", Value = 180 });
				this.Rotations.Add(new() { Label = "270˚ Clockwise", Value = 270 });
				this.SelectedRotation = this.Rotations.ElementAt(0);
			}
			catch
			{
			}

			return Task.CompletedTask;
		}

		protected Task LoadIpAddressesAsync()
		{
			try
			{
				IPHostEntry entry = Dns.GetHostEntry(Dns.GetHostName());
				IPAddress a = entry.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();
				this.IpAddresses.Add(IPAddress.Any);
				this.IpAddresses.Add(IPAddress.Loopback);
				this.IpAddresses.Add(a);
			}
			catch
			{
			}

			return Task.CompletedTask;
		}

		protected Task LoadLabelUnitsAsync()
		{
			try
			{
				this.LabelUnits.Add(new LabelUnit() { Unit = LengthUnit.Inch });
				this.LabelUnits.Add(new LabelUnit() { Unit = LengthUnit.Millimeter });
				this.LabelUnits.Add(new LabelUnit() { Unit = LengthUnit.Centimeter });
				this.SelectedLabelUnit = this.LabelUnits.ElementAt(0);
			}
			catch
			{
			}

			return Task.CompletedTask;
		}

		protected void OnSelectedPrinterConfigurationChanged()
		{
			try
			{
				if (this.SelectedPrinterConfiguration != null)
				{
					this.Name = this.SelectedPrinterConfiguration.Name;
					this.SelectedIpAddress = this.IpAddresses.Where(t => t.ToString() == this.SelectedPrinterConfiguration.HostAddress).SingleOrDefault();
					this.Port = this.SelectedPrinterConfiguration.Port;
					this.LabelWidth = this.SelectedPrinterConfiguration.LabelWidth;
					this.LabelHeight = this.SelectedPrinterConfiguration.LabelHeight;
					this.SelectedLabelUnit = this.LabelUnits.Where(t => t.Unit == (LengthUnit)this.SelectedPrinterConfiguration.LabelUnit).SingleOrDefault();
					this.SelectedResolution = this.Resolutions.Where(t => t.Dpmm == this.SelectedPrinterConfiguration.ResolutionInDpmm).SingleOrDefault();
					this.SelectedRotation = this.Rotations.Where(t => t.Value == this.SelectedPrinterConfiguration.RotationAngle).SingleOrDefault();
					this.ImagePath = this.SelectedPrinterConfiguration.ImagePath;
				}
				else
				{
					this.Name = null;
					this.SelectedIpAddress = null;
					this.Port = 0;
					this.LabelWidth = 0;
					this.LabelHeight = 0;
					this.SelectedLabelUnit = null;
					this.SelectedResolution = null;
					this.SelectedRotation = null;
					this.ImagePath = null;
				}
			}
			finally
			{
				this.LoadFilters();
				this.Changes = false;
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
			finally
			{
				this.RaisePropertyChanged(nameof(this.FilterDescription));
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
		}

		protected async Task UndoCommandAsync()
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

		protected async Task AddCommandAsync()
		{
			//
			// Get a writable repository.
			//
			using IWritableRepository<IPrinterConfiguration> repository = await this.RepositoryFactory.GetWritableAsync<IPrinterConfiguration>();

			IPrinterConfiguration item = await repository.ModelFactory.CreateAsync();
			item.Name = this.GetNewName("New Printer Configuration");
			item.HostAddress = "127.0.0.1";
			item.Port = 9100;
			item.LabelHeight = 6;
			item.LabelWidth = 4;
			item.LabelUnit = (int)LengthUnit.Inch;
			item.ResolutionInDpmm = 8;
			item.RotationAngle = 0;
			item.ImagePath = FileLocations.ImageCache.FullName;

			this.PrinterConfigurations.Add(item);
			this.SelectedPrinterConfiguration = item;
			this.Changes = true;
		}

		protected async Task SaveCommandAsync()
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
					this.SelectedPrinterConfiguration.HostAddress = this.SelectedIpAddress.ToString();
					this.SelectedPrinterConfiguration.Port = this.Port;
					this.SelectedPrinterConfiguration.LabelHeight = this.LabelHeight;
					this.SelectedPrinterConfiguration.LabelWidth = this.LabelWidth;
					this.SelectedPrinterConfiguration.LabelUnit = (int)this.SelectedLabelUnit.Unit;
					this.SelectedPrinterConfiguration.ResolutionInDpmm = this.SelectedResolution.Dpmm;
					this.SelectedPrinterConfiguration.RotationAngle = this.SelectedRotation.Value;
					this.SelectedPrinterConfiguration.ImagePath = this.ImagePath;
					this.SelectedPrinterConfiguration.Filters = JsonConvert.SerializeObject(this.Filters.ToArray());

					//
					// Track the current item.
					//
					int id = this.SelectedPrinterConfiguration.Id;

					//
					// Check if this is an add or update.
					//
					if (this.SelectedPrinterConfiguration.Id == 0)
					{
						(bool result, IPrinterConfiguration newItem) = await repository.AddAsync(context, this.SelectedPrinterConfiguration);
						id = newItem.Id;
					}
					else
					{
						bool result = await repository.UpdateAsync(context, this.SelectedPrinterConfiguration);
					}

					////
					//// Make sure the image path exists.
					////
					//DirectoryInfo dir = new(this.ImagePath);
					//dir.Create();

					//
					// Reload the list and select this object.
					//
					await this.LoadPrinterConfigurationsAsync(id);
				}
			}
		}

		protected async Task DeleteCommandAsync()
		{
			if (this.SelectedPrinterConfiguration != null && this.PrinterConfigurations.Count() > 1)
			{
				if (MessageBox.Show($"Are you sure you want to delete printer configuration '{this.SelectedPrinterConfiguration.Name}'?", "Delete Printer Configuration", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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

						if (await repository.DeleteAsync(context, this.SelectedPrinterConfiguration))
						{
							await this.LoadPrinterConfigurationsAsync();
						}
						else
						{
							MessageBox.Show("Delete failed.", "Delete Printer Configuration", MessageBoxButton.OK, MessageBoxImage.Exclamation);
						}
					}
				}
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

				this.PrinterConfigurations.Add(item);
				this.SelectedPrinterConfiguration = item;
				this.Changes = true;
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
			//catch (Exception ex)
			//{
				
			//}
			finally
			{
				
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
