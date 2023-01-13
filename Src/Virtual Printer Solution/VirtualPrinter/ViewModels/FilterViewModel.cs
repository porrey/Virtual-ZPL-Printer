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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using VirtualPrinter.Events;

namespace VirtualPrinter.ViewModels
{
	public class FilterViewModel : BindableBase
	{
		public FilterViewModel()
		{
			this.AddCommand = new DelegateCommand(async () => await this.AddCommandAsync(), () => true);
			this.DeleteCommand = new DelegateCommand(async () => await this.DeleteCommandAsync(), () => this.Priority > 0);
			this.UpCommand = new DelegateCommand(async () => await this.UpCommandAsync(), () => this.Priority > 1);
			this.DownCommand = new DelegateCommand(async () => await this.DownCommandAsync(), () => this.Priority != this.FilterCount);
		}

		public FilterViewModel(IEventAggregator eventAggregator, int prioirty = 0)
			: this()
		{
			this.EventAggregator = eventAggregator;
			this.Priority = prioirty;
		}

		[JsonIgnore]
		private int FilterCount { get; set; }

		[JsonIgnore]
		private IEventAggregator _eventAggregator = null;

		[JsonIgnore]
		public IEventAggregator EventAggregator
		{
			get
			{
				return _eventAggregator;
			}
			set
			{
				this.SetProperty(ref _eventAggregator, value);
				this.EventAggregator.GetEvent<FilterCountEvent>().Subscribe((e) => this.OnFilterCountChanged(e), ThreadOption.UIThread);
			}
		}

		[JsonIgnore]
		public DelegateCommand AddCommand { get; set; }

		[JsonIgnore]
		public DelegateCommand DeleteCommand { get; set; }

		[JsonIgnore]
		public DelegateCommand UpCommand { get; set; }

		[JsonIgnore]
		public DelegateCommand DownCommand { get; set; }

		private int _priority = 0;
		public int Priority
		{
			get
			{
				return _priority;
			}
			set
			{
				this.SetProperty(ref _priority, value);

				if (this.EventAggregator != null)
				{
					this.EventAggregator.GetEvent<FilterChangeEvent>().Publish(new FilterChangeEventArgs(FilterChangeEventArgs.ActionType.Property, this));
				}

				this.RefreshCommands();
			}
		}

		private string _find = string.Empty;
		public string Find
		{
			get
			{
				return _find;
			}
			set
			{
				this.SetProperty(ref _find, value);

				if (this.Priority == 0 && !string.IsNullOrWhiteSpace(this.Find))
				{
					this.Priority = 1;
				}

				if (this.EventAggregator != null)
				{
					this.EventAggregator.GetEvent<FilterChangeEvent>().Publish(new FilterChangeEventArgs(FilterChangeEventArgs.ActionType.Property, this));
				}

				this.RefreshCommands();
			}
		}

		private string _replace = string.Empty;
		public string Replace
		{
			get
			{
				return _replace;
			}
			set
			{
				this.SetProperty(ref _replace, value);

				if (this.Priority == 0 && !string.IsNullOrWhiteSpace(this.Replace))
				{
					this.Priority = 1;
				}

				if (this.EventAggregator != null)
				{
					this.EventAggregator.GetEvent<FilterChangeEvent>().Publish(new FilterChangeEventArgs(FilterChangeEventArgs.ActionType.Property, this));
				}

				this.RefreshCommands();
			}
		}

		private bool _treatAsRegularExpression = false;
		public bool TreatAsRegularExpression
		{
			get
			{
				return _treatAsRegularExpression;
			}
			set
			{
				this.SetProperty(ref _treatAsRegularExpression, value);

				if (this.EventAggregator != null)
				{
					this.EventAggregator.GetEvent<FilterChangeEvent>().Publish(new FilterChangeEventArgs(FilterChangeEventArgs.ActionType.Property, this));
				}

				this.RefreshCommands();
			}
		}

		public void RefreshCommands()
		{
			//
			// Refresh the state of all of the command buttons.
			//
			this.AddCommand.RaiseCanExecuteChanged();
			this.DeleteCommand.RaiseCanExecuteChanged();
			this.UpCommand.RaiseCanExecuteChanged();
			this.DownCommand.RaiseCanExecuteChanged();
		}

		public void Reset()
		{
			this.Priority = 0;
			this.Find = string.Empty;
			this.Replace = string.Empty;
			this.TreatAsRegularExpression = false;
		}

		protected Task AddCommandAsync()
		{
			try
			{
				if (this.EventAggregator != null)
				{
					this.EventAggregator.GetEvent<FilterChangeEvent>().Publish(new FilterChangeEventArgs(FilterChangeEventArgs.ActionType.Add, this));
				}
			}
			finally
			{
				this.RefreshCommands();
			}

			return Task.CompletedTask;
		}

		protected Task DeleteCommandAsync()
		{
			try
			{
				if (this.EventAggregator != null)
				{
					this.EventAggregator.GetEvent<FilterChangeEvent>().Publish(new FilterChangeEventArgs(FilterChangeEventArgs.ActionType.Delete, this));
				}
			}
			finally
			{
				this.RefreshCommands();
			}

			return Task.CompletedTask;
		}

		protected Task UpCommandAsync()
		{
			try
			{
				if (this.EventAggregator != null)
				{
					this.EventAggregator.GetEvent<FilterChangeEvent>().Publish(new FilterChangeEventArgs(FilterChangeEventArgs.ActionType.Up, this));
				}
			}
			finally
			{
				this.RefreshCommands();
			}

			return Task.CompletedTask;
		}

		protected Task DownCommandAsync()
		{
			try
			{
				if (this.EventAggregator != null)
				{
					this.EventAggregator.GetEvent<FilterChangeEvent>().Publish(new FilterChangeEventArgs(FilterChangeEventArgs.ActionType.Down, this));
				}
			}
			finally
			{
				this.RefreshCommands();
			}

			return Task.CompletedTask;
		}

		private void OnFilterCountChanged(FilterCountEventEventArgs e)
		{
			this.FilterCount = e.Count;
			this.RefreshCommands();
		}

		public static IList<FilterViewModel> ToList(string json)
		{
			IList<FilterViewModel> returnValue = Array.Empty<FilterViewModel>();

			if (!string.IsNullOrWhiteSpace(json))
			{
				returnValue = JsonConvert.DeserializeObject<IList<FilterViewModel>>(json);
			}

			return returnValue;
		}

		public static string ToJson(IList<FilterViewModel> items)
		{
			return JsonConvert.SerializeObject(items.Where(t => t.Priority != 0).ToList(), Formatting.Indented);
		}

		public static FilterViewModel Create(IEventAggregator eventAggregator, int priority = 0)
		{
			return new()
			{
				EventAggregator = eventAggregator,
				Priority = priority,
				Find = null,
				Replace = null,
				TreatAsRegularExpression = false
			};
		}
	}
}
