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
using System.Windows;
using Diamond.Core.Wpf;
using VirtualPrinter.ViewModels;

namespace VirtualPrinter.Views
{
	public partial class GlobalSettingsView : Window
	{
		public GlobalSettingsView(GlobalSettingsViewModel viewModel, IMainWindow mainWindow)
		{
			this.DataContext = viewModel;
			this.InitializeComponent();
			this.Owner = (Window)mainWindow;
		}

		public GlobalSettingsViewModel ViewModel => (GlobalSettingsViewModel)this.DataContext;

		protected override async void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);
			await this.ViewModel.InitializeAsync();
			this.SizeToContent = SizeToContent.WidthAndHeight;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
