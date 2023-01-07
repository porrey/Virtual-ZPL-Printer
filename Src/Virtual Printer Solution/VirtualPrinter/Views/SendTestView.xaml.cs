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
using System.Windows;
using Prism.Events;
using VirtualPrinter.Events;
using VirtualPrinter.ViewModels;

namespace VirtualPrinter.Views
{
	public partial class SendTestView : Window
	{
		public SendTestView(IEventAggregator eventAggregator, SendTestViewModel viewModel)
		{
			this.DataContext = viewModel;
			this.EventAggregator = eventAggregator;
			this.RestoreWindow();
			this.InitializeComponent();
		}

		protected IEventAggregator EventAggregator { get; set; }
		public SendTestViewModel ViewModel => (SendTestViewModel)this.DataContext;

		private void RestoreWindow()
		{
			if (Properties.Settings.Default.SendTestLabelLeft != 0)
			{
				this.Width = Properties.Settings.Default.SendTestLabelWidth;
				this.Height = Properties.Settings.Default.SendTestLabelHeight;
				this.Left = Properties.Settings.Default.SendTestLabelLeft;
				this.Top = Properties.Settings.Default.SendTestLabelTop;
			}
			else
			{
				this.Width = 500;
				this.Height = 500;
			}
		}

		private void SaveWindow()
		{
			Properties.Settings.Default.SendTestLabelWidth = this.Width;
			Properties.Settings.Default.SendTestLabelHeight = this.Height;
			Properties.Settings.Default.SendTestLabelLeft = this.Left;
			Properties.Settings.Default.SendTestLabelTop = this.Top;
			Properties.Settings.Default.Save();
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
	}
}
