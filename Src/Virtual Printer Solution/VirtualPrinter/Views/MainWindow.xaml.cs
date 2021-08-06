using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Diamond.Core.Wpf;
using VirtualZplPrinter.ViewModels;

namespace VirtualZplPrinter.Views
{
	public partial class MainView : Window, IMainWindow
	{
		public MainView(MainViewModel viewModel)
		{
			this.DataContext = viewModel;
			this.RestoreWindow();
			this.InitializeComponent();
		}

		protected MainViewModel ViewModel => (MainViewModel)this.DataContext;

		protected override async void OnInitialized(EventArgs e)
		{
			await ((MainViewModel)this.DataContext).InitializeAsync();
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
				this.ViewModel.Port = Properties.Settings.Default.Port;
				this.ViewModel.LabelHeight = Properties.Settings.Default.LabelHeight;
				this.ViewModel.LabelWidth = Properties.Settings.Default.LabelWidth;
				this.ViewModel.SelectedResolution = this.ViewModel.Resolutions.Where(t => t.Dpmm == Properties.Settings.Default.Dpmm).SingleOrDefault();
				this.ViewModel.SelectedLabelUnit = this.ViewModel.LabelUnits.Where(t => t.Unit == Properties.Settings.Default.LabelUnit).SingleOrDefault();
				this.ViewModel.SelectedIpAddress = this.ViewModel.IpAddresses.Where(t => t.ToString() == Properties.Settings.Default.IpAddress).SingleOrDefault();

				if (Directory.Exists(Properties.Settings.Default.ImagePath))
				{
					this.ViewModel.ImagePath = Properties.Settings.Default.ImagePath;
				}
				else
				{
					this.ViewModel.ImagePath = this.ViewModel.ImageCacheRepository.DefaultFolder;
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
			this.Width = (double)SystemParameters.WorkArea.Width * .65;
			this.Height = (double)SystemParameters.WorkArea.Height * .95;
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

			Properties.Settings.Default.ImagePath = this.ViewModel.ImagePath;
			Properties.Settings.Default.Port = this.ViewModel.Port;
			Properties.Settings.Default.AutoStart = this.ViewModel.AutoStart;
			Properties.Settings.Default.LabelHeight = this.ViewModel.LabelHeight;
			Properties.Settings.Default.LabelWidth = this.ViewModel.LabelWidth;
			Properties.Settings.Default.Dpmm = this.ViewModel.SelectedResolution.Dpmm;
			Properties.Settings.Default.LabelUnit = this.ViewModel.SelectedLabelUnit.Unit;
			Properties.Settings.Default.IpAddress = this.ViewModel.SelectedIpAddress?.ToString();
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
	}
}
