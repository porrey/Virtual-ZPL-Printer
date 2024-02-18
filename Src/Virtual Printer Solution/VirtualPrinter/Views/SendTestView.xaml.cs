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
using Prism.Events;
using VirtualPrinter.ApplicationSettings;
using VirtualPrinter.PublishSubscribe;
using VirtualPrinter.ViewModels;

namespace VirtualPrinter.Views
{
	public partial class SendTestView : Window
	{
		public SendTestView(IEventAggregator eventAggregator, ISettings settings, SendTestViewModel viewModel)
		{
			this.EventAggregator = eventAggregator;
			this.Settings = settings;

			this.DataContext = viewModel;
			this.RestoreWindow();
			this.InitializeComponent();
		}

		protected IEventAggregator EventAggregator { get; set; }
		protected ISettings Settings { get; set; }
		public SendTestViewModel ViewModel => (SendTestViewModel)this.DataContext;

		private void RestoreWindow()
		{
			if (this.Settings.SendTestLabelLeft != 0)
			{
				this.Width = this.Settings.SendTestLabelWidth;
				this.Height = this.Settings.SendTestLabelHeight;
				this.Left = this.Settings.SendTestLabelLeft;
				this.Top = this.Settings.SendTestLabelTop;
			}
			else
			{
				this.Width = 500;
				this.Height = 500;
			}
		}

		private void SaveWindow()
		{
			this.Settings.SendTestLabelWidth = this.Width;
			this.Settings.SendTestLabelHeight = this.Height;
			this.Settings.SendTestLabelLeft = this.Left;
			this.Settings.SendTestLabelTop = this.Top;
			this.Settings.Save();
		}

		protected override async void OnInitialized(EventArgs e)
		{
			await this.ViewModel.InitializeAsync();
			base.OnInitialized(e);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			this.SaveWindow();
			this.EventAggregator.GetEvent<WindowHiddenEvent>().Publish(new WindowHiddenEventArgs() { Window = this });
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
