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
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Diamond.Core.Wpf;
using Prism.Events;
using VirtualPrinter.ApplicationSettings;
using VirtualPrinter.PublishSubscribe;
using VirtualPrinter.ViewModels;

namespace VirtualPrinter.Views
{
	public partial class MainView : Window, IMainWindow
	{
		public MainView(IEventAggregator eventAggregator, ISettings settings, MainViewModel viewModel)
		{
			this.EventAggregator = eventAggregator;
			this.Settings = settings;

			this.DataContext = viewModel;
			this.RestoreWindow();
			this.InitializeComponent();

			this.ListView.MouseDoubleClick += this.ListView_MouseDoubleClick;

			this.Timer = new DispatcherTimer();
			this.Timer.Tick += this.Timer_Tick;
			this.Timer.Interval = TimeSpan.FromSeconds(15);
			this.Timer.Start();
		}

		protected IEventAggregator EventAggregator { get; set; }
		protected ISettings Settings { get; set; }

		private void Timer_Tick(object sender, EventArgs e)
		{
			this.EventAggregator.GetEvent<TimerEvent>().Publish(new TimerEventArgs());
		}

		protected MainViewModel ViewModel => (MainViewModel)this.DataContext;
		protected DispatcherTimer Timer { get; set; }

		protected override async void OnInitialized(EventArgs e)
		{
			await this.ViewModel.InitializeAsync();
			base.OnInitialized(e);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			this.ViewModel.SendTestView?.Close();
			this.SaveWindow();
			base.OnClosing(e);
		}

		private void RestoreWindow()
		{
			try
			{
				this.WindowState = (WindowState)this.Settings.WindowState;
				this.ViewModel.AutoStart = this.Settings.AutoStart;
				this.ViewModel.SelectedPrinterConfiguration = this.ViewModel.PrinterConfigurations.Where(t => t.Id == this.Settings.PrinterConfiguration).SingleOrDefault();

				if (this.ViewModel.SelectedPrinterConfiguration == null)
				{
					this.ViewModel.SelectedPrinterConfiguration = this.ViewModel.PrinterConfigurations.FirstOrDefault();
				}

				if (this.Settings.Initialized)
				{
					this.Width = this.Settings.Width;
					this.Height = this.Settings.Height;
					this.Left = this.Settings.Left;
					this.Top = this.Settings.Top;
				}
				else
				{
					this.WindowDefaults();
				}
			}
			catch
			{
				this.WindowDefaults();
			}
		}

		private void WindowDefaults()
		{
			this.Width = (double)SystemParameters.WorkArea.Width * .55;
			this.Height = (double)SystemParameters.WorkArea.Height * .85;
			this.Left = ((double)SystemParameters.WorkArea.Width - this.Width) / 2.0;
			this.Top = .85 * ((double)SystemParameters.WorkArea.Height - this.Height) / 2.0;
			this.SaveWindow();
		}

		private void SaveWindow()
		{
			if (this.WindowState == WindowState.Normal ||
				this.WindowState == WindowState.Minimized)
			{
				this.Settings.Width = this.Width;
				this.Settings.Height = this.Height;
				this.Settings.Left = this.Left;
				this.Settings.Top = this.Top;
				this.Settings.WindowState = (int)this.WindowState;
			}
			else
			{
				this.Settings.Width = this.RestoreBounds.Width;
				this.Settings.Height = this.RestoreBounds.Height;
				this.Settings.Left = this.RestoreBounds.Left;
				this.Settings.Top = this.RestoreBounds.Top;
				this.Settings.WindowState = (int)WindowState.Maximized;
			}

			if (this.ViewModel.SelectedPrinterConfiguration != null)
			{
				this.Settings.PrinterConfiguration = this.ViewModel.SelectedPrinterConfiguration.Id;
			}
			else
			{
				this.Settings.PrinterConfiguration = 1;
			}

			this.Settings.AutoStart = this.ViewModel.AutoStart;
			this.Settings.Initialized = true;
			this.Settings.Save();
		}

		private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (sender is ListView listView)
			{
				if (e.AddedItems.Count > 0)
				{
					listView.ScrollIntoView(e.AddedItems[e.AddedItems.Count - 1]);
				}
			}
		}

		private void ContextMenu_Opened(object sender, RoutedEventArgs e)
		{
			this.ViewModel.RefreshCommands();
		}

		private async void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (((FrameworkElement)e.OriginalSource).DataContext is StoredImageViewModel item)
			{
				await this.ViewModel.LabelPreviewAsync(item);
			}
		}
	}
}
