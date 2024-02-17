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
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using VirtualPrinter.FontService;
using VirtualPrinter.PublishSubscribe;

namespace VirtualPrinter.ViewModels
{
	public class FontManagerViewModel : BindableBase
	{
		public FontManagerViewModel(ILogger<FontManagerViewModel> logger, IEventAggregator eventAggregator, IFontService fontService)
			: base()
		{
			this.Logger = logger;
			this.EventAggregator = eventAggregator;
			this.FontService = fontService;

			this.OkCommand = new(async () => await this.OkCommandAsync(), () => this.EnableOkCommand());
			this.CancelCommand = new(async () => await this.CancelCommandAsync(), () => true);
			this.AddCommand = new(async () => await this.AddCommandAsync(), () => true);

			this.EventAggregator.GetEvent<ChangeEvent<FontViewModel>>().Subscribe((e) => this.DeleteFont(e.Item), ThreadOption.UIThread, false, t => t.Action == ChangeActionType.Delete);
			this.EventAggregator.GetEvent<ChangeEvent<FontViewModel>>().Subscribe((e) => this.RefreshCommands(), ThreadOption.UIThread, false, t => t.Action == ChangeActionType.Refresh);
			this.EventAggregator.GetEvent<ChangeEvent<FontViewModel>>().Subscribe((e) => this.SaveFont(e.Item), ThreadOption.UIThread, false, t => t.Action == ChangeActionType.Save);
		}

		private void OkCommand_CanExecuteChanged(object sender, EventArgs e) => throw new NotImplementedException();

		protected ILogger<FontManagerViewModel> Logger { get; set; }
		protected IEventAggregator EventAggregator { get; set; }
		protected IFontService FontService { get; set; }

		public DelegateCommand OkCommand { get; set; }
		public DelegateCommand CancelCommand { get; set; }
		public DelegateCommand AddCommand { get; set; }

		public ObservableCollection<FontViewModel> Fonts { get; } = [];

		private PrinterConfigurationViewModel _printerConfiguration = null;
		public PrinterConfigurationViewModel PrinterConfiguration
		{
			get
			{
				return this._printerConfiguration;
			}
			set
			{
				this.SetProperty(ref this._printerConfiguration, value);
				this.RefreshCommands();
			}
		}

		private FontViewModel _selectedFont = null;
		public FontViewModel SelectedFont
		{
			get
			{
				return this._selectedFont;
			}
			set
			{
				this.SetProperty(ref this._selectedFont, value);
				this.RefreshCommands();
			}
		}

		private string _buttonText = "Close";
		public string ButtonText
		{
			get
			{
				return this._buttonText;
			}
			set
			{
				this.SetProperty(ref this._buttonText, value);
			}
		}

		public async Task InitializeAsync()
		{
			try
			{
				await this.LoadFontsAsync();
				this.RefreshCommands();
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, $"Exception in {nameof(FontManagerViewModel)}.{nameof(this.InitializeAsync)}");
				MessageBox.Show(ex.Message, "Initialize Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}
		}

		protected async Task LoadFontsAsync()
		{
			try
			{
				//
				// Clear the list.
				//
				this.Fonts.Clear();

				//
				// Get the fonts.
				//
				IEnumerable<IPrinterFont> fonts = await this.FontService.GetFontsAsync();

				//
				// Load each template.
				//
				foreach (IPrinterFont font in fonts)
				{
					FontViewModel item = new(this.EventAggregator, this.FontService, font);
					this.Fonts.Add(item);
				}
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, $"Exception in {nameof(FontManagerViewModel)}.{nameof(this.LoadFontsAsync)}");
				MessageBox.Show(ex.Message, "Load Fonts Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}
		}

		protected async Task OkCommandAsync()
		{
			try
			{
				foreach (FontViewModel font in this.Fonts)
				{
					await font.SaveAsync();
				}
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, $"Exception in {nameof(FontManagerViewModel)}.{nameof(this.OkCommandAsync)}");
				MessageBox.Show(ex.Message, "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}
		}

		protected Task CancelCommandAsync()
		{
			return Task.CompletedTask;
		}

		public void RefreshCommands()
		{
			//
			// Refresh the state of all of the command buttons.
			//
			this.OkCommand.RaiseCanExecuteChanged();
			this.CancelCommand.RaiseCanExecuteChanged();

			int changedCount = (from tbl in this.Fonts
								where tbl.Changed
								select tbl).Count();

			this.ButtonText = changedCount > 0 ? "Cancel" : "Close";
		}

		protected void DeleteFont(FontViewModel font)
		{
			try
			{
				font.DeleteAsync();
				this.Fonts.Remove(font);
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, $"Exception in {nameof(FontManagerViewModel)}.{nameof(this.DeleteFont)}");
				MessageBox.Show(ex.Message, "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}
		}

		protected async void SaveFont(FontViewModel font)
		{
			try
			{
				await font.SaveAsync();
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, $"Exception in {nameof(FontManagerViewModel)}.{nameof(this.SaveFont)}");
				MessageBox.Show(ex.Message, "Save Font", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}
		}

		protected async Task AddCommandAsync()
		{
			try
			{
				FontViewModel model = new(this.EventAggregator, this.FontService, await this.FontService.CreateAsync())
				{
					PrinterDevice = "R"
				};

				this.Fonts.Add(model);
				this.SelectedFont = model;
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, $"Exception in {nameof(FontManagerViewModel)}.{nameof(this.AddCommandAsync)}");
				MessageBox.Show(ex.Message, "Add Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}
		}

		protected bool EnableOkCommand()
		{
			bool returnValue = false;

			try
			{
				int changedCount = (from tbl in this.Fonts
									where tbl.Changed
									select tbl).Count();

				int saveCount = (from tbl in this.Fonts
								 where tbl.CanSave
								 select tbl).Count();

				returnValue = (saveCount == changedCount) && changedCount > 0;
			}
			catch (Exception ex)
			{
				this.Logger.LogError(ex, $"Exception in {nameof(FontManagerViewModel)}.{nameof(this.EnableOkCommand)}");
				MessageBox.Show(ex.Message, "Enable OK Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}

			return returnValue;
		}
	}
}
