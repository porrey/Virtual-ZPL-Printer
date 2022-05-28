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
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Diamond.Core.Wpf;
using ImageCache.Abstractions;
using Prism.Events;
using VirtualZplPrinter.Events;
using VirtualZplPrinter.ViewModels;

namespace VirtualZplPrinter.Views
{
	public partial class MainView : Window, IMainWindow
	{
		public MainView(IEventAggregator eventAggregator, MainViewModel viewModel)
		{
			this.EventAggregator = eventAggregator;
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
			this.SaveWindow();
			base.OnClosing(e);
		}

		private void RestoreWindow()
		{
			try
			{
				this.WindowState = (WindowState)Properties.Settings.Default.WindowState;
				this.ViewModel.AutoStart = Properties.Settings.Default.AutoStart;
				this.ViewModel.SelectedPrinterConfiguration = this.ViewModel.PrinterConfigurations.Where(t => t.Id == Properties.Settings.Default.PrinterConfiguration).SingleOrDefault();

				if (this.ViewModel.SelectedPrinterConfiguration == null)
				{
					this.ViewModel.SelectedPrinterConfiguration = this.ViewModel.PrinterConfigurations.FirstOrDefault();
				}

				if (Properties.Settings.Default.Initialized)
				{
					this.Width = Properties.Settings.Default.Width;
					this.Height = Properties.Settings.Default.Height;
					this.Left = Properties.Settings.Default.Left;
					this.Top = Properties.Settings.Default.Top;
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
				Properties.Settings.Default.Width = this.Width;
				Properties.Settings.Default.Height = this.Height;
				Properties.Settings.Default.Left = this.Left;
				Properties.Settings.Default.Top = this.Top;
				Properties.Settings.Default.WindowState = (int)this.WindowState;
			}
			else
			{
				Properties.Settings.Default.Width = this.RestoreBounds.Width;
				Properties.Settings.Default.Height = this.RestoreBounds.Height;
				Properties.Settings.Default.Left = this.RestoreBounds.Left;
				Properties.Settings.Default.Top = this.RestoreBounds.Top;
				Properties.Settings.Default.WindowState = (int)WindowState.Maximized;
			}

			if (this.ViewModel.SelectedPrinterConfiguration != null)
			{
				Properties.Settings.Default.PrinterConfiguration = this.ViewModel.SelectedPrinterConfiguration.Id;
			}
			else
			{
				Properties.Settings.Default.PrinterConfiguration = 1;
			}

			Properties.Settings.Default.AutoStart = this.ViewModel.AutoStart;
			Properties.Settings.Default.Initialized = true;
			Properties.Settings.Default.Save();
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
			if (((FrameworkElement)e.OriginalSource).DataContext is IStoredImage item)
			{
				await this.ViewModel.LabelPreviewAsync(item);
			}
		}
	}
}
