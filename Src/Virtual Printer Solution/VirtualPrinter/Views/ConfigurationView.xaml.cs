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
using System.Windows.Controls;
using Diamond.Core.Wpf;
using VirtualZplPrinter.ViewModels;

namespace VirtualZplPrinter.Views
{
	public partial class ConfigurationView : Window, IMainWindow
	{
		public ConfigurationView(ConfigurationViewModel viewModel, IMainWindow mainWindow)
		{
			this.Owner = (Window)mainWindow;
			this.DataContext = viewModel;
			this.InitializeComponent();
		}

		protected ConfigurationViewModel ViewModel => (ConfigurationViewModel)this.DataContext;

		protected override async void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);
			await this.ViewModel.InitializeAsync();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
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

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
