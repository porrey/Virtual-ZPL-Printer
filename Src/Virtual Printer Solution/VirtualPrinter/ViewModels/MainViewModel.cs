using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using VirtualPrinter.Events;
using VirtualPrinter.Models;

namespace VirtualPrinter.ViewModels
{
	public class MainViewModel : BindableBase
	{
		public MainViewModel(IEventAggregator eventAggregator)
		{
			this.EventAggregator = eventAggregator;

			this.Resolutions.Add(new Resolution() { Dpmm = 6 });
			this.Resolutions.Add(new Resolution() { Dpmm = 8 });
			this.Resolutions.Add(new Resolution() { Dpmm = 12 });
			this.Resolutions.Add(new Resolution() { Dpmm = 24 });
			this.SelectedResolution = this.Resolutions.ElementAt(1);

			this.StartCommand = new DelegateCommand(() => _ = this.StartAsync(), () => !this.IsBusy && !this.IsRunning);
			this.StopCommand = new DelegateCommand(() => _ = this.StopAsync(), () => !this.IsBusy && this.IsRunning);
			this.PreviousLabelCommand = new DelegateCommand(() => _ = this.PreviousLabelAsync(), () => !this.IsBusy && this.SelectedLabelIndex > 0);
			this.NextLabelCommand = new DelegateCommand(() => _ = this.NextLabelAsync(), () => !this.IsBusy && this.SelectedLabelIndex < this.Labels.Count - 1);
			this.RemoveLabelCommand = new DelegateCommand(() => _ = this.RemoveLabelAsync(), () => !this.IsBusy && this.SelectedLabel != null);
			this.ClearLabelsCommand = new DelegateCommand(() => _ = this.ClearLabelsAsync(), () => !this.IsBusy && this.Labels.Count > 0);

			//
			// Subscribe to the running state changed event to update the running
			// status of the UI.
			//
			this.EventAggregator.GetEvent<RunningStateChangedEvent>().Subscribe((a) => this.IsRunning = a.IsRunning, ThreadOption.UIThread);
			
			//
			// Subscribe to the label created event to add all new labels
			// to the UI.
			//
			this.EventAggregator.GetEvent<LabelCreatedEvent>().Subscribe((a) => 
			{
				this.Labels.Add(a.Label);
				this.SelectedLabel = a.Label;
				this.ImagePath = a.Label.ImagePath;
			}, ThreadOption.UIThread);
		}

		public async Task InitializeAsync()
		{
			if (this.AutoStart)
			{
				await this.StartAsync();
			}
		}

		protected IEventAggregator EventAggregator { get; set; }
		public ObservableCollection<Resolution> Resolutions { get; } = new ObservableCollection<Resolution>();
		public ObservableCollection<Label> Labels { get; } = new ObservableCollection<Label>();
		public DelegateCommand StartCommand { get; set; }
		public DelegateCommand StopCommand { get; set; }
		public DelegateCommand PreviousLabelCommand { get; set; }
		public DelegateCommand NextLabelCommand { get; set; }
		public DelegateCommand RemoveLabelCommand { get; set; }
		public DelegateCommand ClearLabelsCommand { get; set; }

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

		private Label _selectedLabel = null;
		public Label SelectedLabel
		{
			get
			{
				return _selectedLabel;
			}
			set
			{
				this.SetProperty(ref _selectedLabel, value);
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
		public int LabelWidth
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
		public int LabelHeight
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
			}
		}

		private string _statusText = "No labels";
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

		protected void RefreshCommands()
		{
			this.StartCommand.RaiseCanExecuteChanged();
			this.StopCommand.RaiseCanExecuteChanged();
			this.PreviousLabelCommand.RaiseCanExecuteChanged();
			this.NextLabelCommand.RaiseCanExecuteChanged();
			this.RemoveLabelCommand.RaiseCanExecuteChanged();
			this.ClearLabelsCommand.RaiseCanExecuteChanged();
		}

		protected Task StartAsync()
		{
			this.EventAggregator.GetEvent<StartEvent>().Publish(new StartEventArgs()
			{
				LabelConfiguration = new LabelConfiguration()
				{
					Dpmm = this.SelectedResolution.Dpmm,
					LabelHeight = this.LabelHeight,
					LabelWidth = this.LabelWidth
				},
				Port = this.Port
			});

			return Task.CompletedTask;
		}

		protected Task StopAsync()
		{
			this.EventAggregator.GetEvent<StopEvent>().Publish(new StopEventArgs()
			{
			});

			return Task.CompletedTask;
		}

		protected Task PreviousLabelAsync()
		{

			return Task.CompletedTask;
		}

		protected Task NextLabelAsync()
		{
			return Task.CompletedTask;
		}

		protected Task RemoveLabelAsync()
		{
			int i = this.SelectedLabelIndex;
			this.Labels.Remove(this.SelectedLabel);

			if (this.Labels.Count > 0)
			{
				if (i > 0 && i < this.Labels.Count)
				{
					this.SelectedLabel = this.Labels.ElementAt(i);
				}
				else
				{
					this.SelectedLabel = this.Labels.ElementAt(this.Labels.Count - 1);
				}
			}
			else
			{
				this.SelectedLabel = null;
			}

			return Task.CompletedTask;
		}

		protected Task ClearLabelsAsync()
		{
			this.Labels.Clear();
			return Task.CompletedTask;
		}

		protected int SelectedLabelIndex
		{
			get
			{
				int returnValue = -1;

				returnValue = this.Labels.IndexOf(this.SelectedLabel);

				return returnValue;
			}
		}
	}
}
