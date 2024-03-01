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
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using VirtualPrinter.PublishSubscribe;

namespace VirtualPrinter.ViewModels
{
	public class FilterViewModel : BindableBase
	{
		public FilterViewModel()
		{
			this.AddCommand = new(async () => await this.AddCommandAsync(), () => true);
			this.DeleteCommand = new(async () => await this.DeleteCommandAsync(), () => this.Priority != 0 && this.Priority > 0);
			this.UpCommand = new(async () => await this.UpCommandAsync(), () => this.Priority != 0 && this.Priority > 1);
			this.DownCommand = new(async () => await this.DownCommandAsync(), () => this.Priority != 0 && this.Priority != this.FilterCount);
		}

		public FilterViewModel(IEventAggregator eventAggregator, int priority = 0)
			: this()
		{
			this.EventAggregator = eventAggregator;
			this.Priority = priority;
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
				return this._eventAggregator;
			}
			set
			{
				this.SetProperty(ref this._eventAggregator, value);
				this.EventAggregator.GetEvent<ChangeEvent<int>>().Subscribe((e) => this.OnChangeEvent(e), ThreadOption.UIThread, false, t => t.Action == ChangeActionType.Count);
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
				return this._priority;
			}
			set
			{
				this.SetProperty(ref this._priority, value);
				this.EventAggregator?.GetEvent<ChangeEvent<FilterViewModel>>().Publish(new ChangeEventArgs<FilterViewModel>(ChangeActionType.Property, this));
				this.RefreshCommands();
			}
		}

		private string _find = string.Empty;
		public string Find
		{
			get
			{
				return this._find;
			}
			set
			{
				this.SetProperty(ref this._find, value);

				if (this.Priority == 0 && !string.IsNullOrWhiteSpace(this.Find))
				{
					this.Priority = 1;
				}

				this.EventAggregator?.GetEvent<ChangeEvent<FilterViewModel>>().Publish(new ChangeEventArgs<FilterViewModel>(ChangeActionType.Property, this));
				this.RefreshCommands();
			}
		}

		private string _replace = string.Empty;
		public string Replace
		{
			get
			{
				return this._replace;
			}
			set
			{
				this.SetProperty(ref this._replace, value);

				if (this.Priority == 0 && !string.IsNullOrWhiteSpace(this.Replace))
				{
					this.Priority = 1;
				}

				this.EventAggregator?.GetEvent<ChangeEvent<FilterViewModel>>().Publish(new ChangeEventArgs<FilterViewModel>(ChangeActionType.Property, this));
				this.RefreshCommands();
			}
		}

		private bool _treatAsRegularExpression = false;
		public bool TreatAsRegularExpression
		{
			get
			{
				return this._treatAsRegularExpression;
			}
			set
			{
				this.SetProperty(ref this._treatAsRegularExpression, value);

				this.EventAggregator?.GetEvent<ChangeEvent<FilterViewModel>>().Publish(new ChangeEventArgs<FilterViewModel>(ChangeActionType.Property, this));
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
				this.EventAggregator?.GetEvent<ChangeEvent<FilterViewModel>>().Publish(new ChangeEventArgs<FilterViewModel>(ChangeActionType.Add, this));
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
				this.EventAggregator?.GetEvent<ChangeEvent<FilterViewModel>>().Publish(new ChangeEventArgs<FilterViewModel>(ChangeActionType.Delete, this));
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
				this.EventAggregator?.GetEvent<ChangeEvent<FilterViewModel>>().Publish(new ChangeEventArgs<FilterViewModel>(ChangeActionType.Up, this));
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
				this.EventAggregator?.GetEvent<ChangeEvent<FilterViewModel>>().Publish(new ChangeEventArgs<FilterViewModel>(ChangeActionType.Down, this));
			}
			finally
			{
				this.RefreshCommands();
			}

			return Task.CompletedTask;
		}

		private void OnChangeEvent(ChangeEventArgs<int> e)
		{
			this.FilterCount = e.Item;
			this.RefreshCommands();
		}

		public static IList<FilterViewModel> ToList(string json)
		{
			IList<FilterViewModel> returnValue = [];

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
