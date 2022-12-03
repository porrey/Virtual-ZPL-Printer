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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using VirtualPrinter.Db.Abstractions;
using VirtualPrinter.Events;

namespace VirtualPrinter.ViewModels
{
	public class EditFiltersViewModel : BindableBase
	{
		public EditFiltersViewModel(IEventAggregator eventAggregator)
			: base()
		{
			this.EventAggregator = eventAggregator;
			this.OkCommand = new DelegateCommand(async () => await this.OkCommandAsync(), () => !this.Filters.Where(t => string.IsNullOrEmpty(t.Find)).Any());
			this.CancelCommand = new DelegateCommand(async () => await this.CancelCommandAsync(), () => true);

			this.EventAggregator.GetEvent<FilterChangeEvent>().Subscribe((e) => this.OnFilterChangedEvent(e), ThreadOption.UIThread);
		}

		protected IEventAggregator EventAggregator { get; set; }

		public DelegateCommand OkCommand { get; set; }
		public DelegateCommand CancelCommand { get; set; }

		public ObservableCollection<FilterViewModel> Filters { get; } = new ObservableCollection<FilterViewModel>();

		private IPrinterConfiguration _printerConfiguration = null;
		public IPrinterConfiguration PrinterConfiguration
		{
			get
			{
				return _printerConfiguration;
			}
			set
			{
				this.SetProperty(ref _printerConfiguration, value);
				this.OnSelectedPrinterConfigurationChanged();
				this.RefreshCommands();
			}
		}

		private FilterViewModel _selectedFilter = null;
		public FilterViewModel SelectedFilter
		{
			get
			{
				return _selectedFilter;
			}
			set
			{
				this.SetProperty(ref _selectedFilter, value);
				this.RefreshCommands();
			}
		}

		private bool _updated = false;
		public bool Updated
		{
			get
			{
				return _updated;
			}
			set
			{
				this.SetProperty(ref _updated, value);
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
				this.EventAggregator.GetEvent<FilterCountEvent>().Publish(new(this.Filters.Count));
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
				this.EventAggregator.GetEvent<FilterCountEvent>().Publish(new(this.Filters.Count));
				this.RefreshCommands();
			}
		}

		protected Task OkCommandAsync()
		{
			this.PrinterConfiguration.Filters = FilterViewModel.ToJson(this.Filters.ToList());
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

		private void OnFilterChangedEvent(FilterChangeEventArgs e)
		{
			try
			{
				switch (e.Action)
				{
					case FilterChangeEventArgs.ActionType.Add:
						{
							this.Filters.Insert(e.FilterItem.Priority, new(this.EventAggregator, this.Filters.Count + 1));
							this.RenumberList();
						}
						break;
					case FilterChangeEventArgs.ActionType.Delete:
						{
							if (this.Filters.Count > 1)
							{
								this.Filters.Remove(e.FilterItem);
								this.RenumberList();
							}
							else
							{
								e.FilterItem.Reset();
							}
						}
						break;
					case FilterChangeEventArgs.ActionType.Up:
						{
							int index = this.Filters.IndexOf(e.FilterItem);
							this.Filters.Remove(e.FilterItem);
							this.Filters.Insert(index - 1, e.FilterItem);
							this.RenumberList();
						}
						break;
					case FilterChangeEventArgs.ActionType.Down:
						{
							int index = this.Filters.IndexOf(e.FilterItem);
							this.Filters.Remove(e.FilterItem);
							this.Filters.Insert(index + 1, e.FilterItem);
							this.RenumberList();
						}
						break;
				}
			}
			finally
			{
				this.EventAggregator.GetEvent<FilterCountEvent>().Publish(new(this.Filters.Count));
				this.RefreshCommands();
			}
		}
	}
}
