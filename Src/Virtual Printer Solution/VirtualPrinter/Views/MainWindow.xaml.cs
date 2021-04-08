using System;
using System.ComponentModel;
using System.Windows;
using Diamond.Core.Wpf;
using VirtualPrinter.ViewModels;

namespace VirtualPrinter.Views
{
	public partial class MainView : Window, IMainWindow
	{
		public MainView(MainViewModel viewModel)
		{
			this.DataContext = viewModel;
			this.InitializeComponent();
			this.RestoreWindow();
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
				if (Properties.Settings.Default.Initialized)
				{
					this.Width = Properties.Settings.Default.Width;
					this.Height = Properties.Settings.Default.Height;
					this.Left = Properties.Settings.Default.Left;
					this.Top = Properties.Settings.Default.Top;
					this.WindowState = (WindowState)Properties.Settings.Default.WindowState;
					this.ViewModel.AutoStart = Properties.Settings.Default.AutoStart;
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

			Properties.Settings.Default.AutoStart = this.ViewModel.AutoStart;
			Properties.Settings.Default.Initialized = true;
			Properties.Settings.Default.Save();
		}
	}
}
