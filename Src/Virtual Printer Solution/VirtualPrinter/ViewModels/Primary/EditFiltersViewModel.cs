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
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using VirtualPrinter.PublishSubscribe;

namespace VirtualPrinter.ViewModels
{
	public class EditFiltersViewModel : BindableBase
	{
		public EditFiltersViewModel(IEventAggregator eventAggregator)
			: base()
		{
			this.EventAggregator = eventAggregator;
			this.OkCommand = new(async () => await this.OkCommandAsync(), () => true);
			this.CancelCommand = new(async () => await this.CancelCommandAsync(), () => true);

			this.EventAggregator.GetEvent<ChangeEvent<FilterViewModel>>().Subscribe((e) => this.OnFilterChangedEvent(e), ThreadOption.UIThread);
		}

		protected IEventAggregator EventAggregator { get; set; }

		public DelegateCommand OkCommand { get; set; }
		public DelegateCommand CancelCommand { get; set; }

		public ObservableCollection<FilterViewModel> Filters { get; } = [];

		private PrinterConfigurationViewModel _printerConfiguration = null;
		public PrinterConfigurationViewModel PrinterConfiguration
		{
			get
			{
				return this._printerConfiguration;
			}
			set
			{
				this.SetProperty(ref this._printerConfiguration, value);
				this.OnSelectedPrinterConfigurationChanged();
				this.RefreshCommands();
			}
		}

		private FilterViewModel _selectedFilter = null;
		public FilterViewModel SelectedFilter
		{
			get
			{
				return this._selectedFilter;
			}
			set
			{
				this.SetProperty(ref this._selectedFilter, value);
				this.RefreshCommands();
			}
		}

		private bool _updated = false;
		public bool Updated
		{
			get
			{
				return this._updated;
			}
			set
			{
				this.SetProperty(ref this._updated, value);
				this.RefreshCommands();
			}
		}

		public Task InitializeAsync()
		{
			this.RefreshCommands();
			return Task.CompletedTask;
		}

		protected void OnSelectedPrinterConfigurationChanged()
		{
			try
			{
				this.LoadFilters();

				if (!this.Filters.Any())
				{
					this.Filters.Add(FilterViewModel.Create(this.EventAggregator));
				}
			}
			finally
			{
				this.EventAggregator.GetEvent<ChangeEvent<int>>().Publish(new(ChangeActionType.Count, this.Filters.Count));
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

				if (this.PrinterConfiguration != null)
				{
					IList<FilterViewModel> filters = FilterViewModel.ToList(this.PrinterConfiguration.Filters);

					foreach (FilterViewModel filter in filters)
					{
						filter.EventAggregator = this.EventAggregator;
					}

					this.Filters.AddRange(filters);
				}
			}
			finally
			{
				this.EventAggregator.GetEvent<ChangeEvent<int>>().Publish(new(ChangeActionType.Count, this.Filters.Count));
				this.RefreshCommands();
			}
		}

		protected Task OkCommandAsync()
		{
			this.PrinterConfiguration.Filters = FilterViewModel.ToJson([.. this.Filters]);
			this.Updated = true;
			return Task.CompletedTask;
		}

		protected Task CancelCommandAsync()
		{
			this.Updated = false;
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

		private void RenumberList()
		{
			int priority = 1;
			foreach (FilterViewModel filter in this.Filters)
			{
				filter.Priority = priority++;
			}
		}

		private void OnFilterChangedEvent(ChangeEventArgs<FilterViewModel> e)
		{
			try
			{
				switch (e.Action)
				{
					case ChangeActionType.Add:
						{
							this.RenumberList();
							this.Filters.Insert(e.Item.Priority, new(this.EventAggregator, this.Filters.Count + 1));
							this.RenumberList();
						}
						break;
					case ChangeActionType.Delete:
						{
							if (this.Filters.Count > 1)
							{
								this.Filters.Remove(e.Item);
								this.RenumberList();
							}
							else
							{
								e.Item.Reset();
							}
						}
						break;
					case ChangeActionType.Up:
						{
							int index = this.Filters.IndexOf(e.Item);
							this.Filters.Remove(e.Item);
							this.Filters.Insert(index - 1, e.Item);
							this.RenumberList();
						}
						break;
					case ChangeActionType.Down:
						{
							int index = this.Filters.IndexOf(e.Item);
							this.Filters.Remove(e.Item);
							this.Filters.Insert(index + 1, e.Item);
							this.RenumberList();
						}
						break;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.EventAggregator.GetEvent<ChangeEvent<int>>().Publish(new(ChangeActionType.Count, this.Filters.Count));
				this.RefreshCommands();
			}
		}
	}
}
