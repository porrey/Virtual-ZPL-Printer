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
using System.IO;
using System.Net.Http;
using System.Windows;
using Humanizer;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using VirtualPrinter.FontService;
using VirtualPrinter.PublishSubscribe;

namespace VirtualPrinter.ViewModels
{
	public class FontViewModel : BindableBase
	{
		public FontViewModel(IEventAggregator eventAggregator, IFontService fontService, IPrinterFont printerFont)
		{
			this.BrowseCommand = new(async () => await this.BrowseCommandAsync(), () => true);
			this.DeleteCommand = new(async () => await this.DeleteCommandAsync(), () => true);
			this.SaveCommand = new(async () => await this.SaveCommandAsync(), () => this.CanSave);
			this.UpdateCommand = new(async () => await this.UpdateCommandAsync(), () => this.CanUpdate);

			this.EventAggregator = eventAggregator;
			this.FontService = fontService;

			try
			{
				this.Loading = true;
				this.FontDetailsFile = printerFont.BaseFileName;
				this.FontName = printerFont.FontName;
				this.PrinterDevice = printerFont.PrinterDevice;
				this.Chars = printerFont.Chars;
				this.FontByteLength = printerFont.FontByteLength;
				this.FontSource = printerFont.FontSource;
			}
			finally
			{
				this.Loading = false;
				this.Changed = false;
				this.RefreshCommands();
			}
		}

		protected IEventAggregator EventAggregator { get; set; }
		protected bool Loading { get; set; }
		protected IFontService FontService { get; set; }

		public ObservableCollection<string> PrinterDevices { get; } = ["R", "B", "E", "A"];

		public DelegateCommand BrowseCommand { get; set; }
		public DelegateCommand DeleteCommand { get; set; }
		public DelegateCommand SaveCommand { get; set; }
		public DelegateCommand UpdateCommand { get; set; }

		public FileInfo DetailsFile => new($"{this.FontService.FontDirectory.FullName}/{this.FontDetailsFile}.json");
		public FileInfo FontDataFile => new($"{this.FontService.FontDirectory.FullName}/{this.FontDetailsFile}.ZPL");

		private string _fontDetailsFile = null;
		public string FontDetailsFile
		{
			get
			{
				return this._fontDetailsFile;
			}
			set
			{
				this.SetProperty(ref this._fontDetailsFile, value);
				this.Changed = true;
				this.RefreshCommands();
			}
		}

		/// <summary>
		/// Regular expression: [A-Z0-9]{1,16}.TTF
		/// </summary>
		private string _fontName = null;
		public string FontName
		{
			get
			{
				return this._fontName;
			}
			set
			{
				this.SetProperty(ref this._fontName, value);
				this.Changed = true;
				this.RefreshCommands();
			}
		}

		/// <summary>
		/// Regular expression: [REBA]
		/// </summary>
		private string _printerDrive = null;
		public string PrinterDevice
		{
			get
			{
				return this._printerDrive;
			}
			set
			{
				this.SetProperty(ref this._printerDrive, value);
				this.Changed = true;
				this.RefreshCommands();
			}
		}

		private string _chars = null;
		public string Chars
		{
			get
			{
				return this._chars;
			}
			set
			{
				this.SetProperty(ref this._chars, value);

				if (!this.Loading && File.Exists(this.FontSource))
				{
					_ = this.LoadFontAsync(this.FontSource);
				}

				this.Changed = true;
				this.RefreshCommands();
			}
		}

		private int _fontByteLength = 0;
		public int FontByteLength
		{
			get
			{
				return this._fontByteLength;
			}
			set
			{
				this.SetProperty(ref this._fontByteLength, value);
				this.Changed = true;
				this.RefreshCommands();
				this.RaisePropertyChanged(nameof(this.SizeDescription));
			}
		}

		private string _fontData = null;
		public string FontData
		{
			get
			{
				return this._fontData;
			}
			set
			{
				this.SetProperty(ref this._fontData, value);
				this.Changed = true;
				this.RefreshCommands();
			}
		}

		private string _fontSource = null;
		public string FontSource
		{
			get
			{
				return this._fontSource;
			}
			set
			{
				this.SetProperty(ref this._fontSource, value);
				this.Changed = true;
				this.RaisePropertyChanged(nameof(this.FontSourceDescription));
				this.RefreshCommands();
			}
		}

		public string FontSourceDescription => this.CanUpdate ? $"{this.FontSource} [{Properties.Strings.Font_Source_Found}]" : $"{this.FontSource} [{Properties.Strings.Font_Source_Missing}]";

		public bool CanSave
		{
			get
			{
				bool returnValue = false;

				if (this.Changed)
				{
					returnValue = !string.IsNullOrWhiteSpace(this.FontDetailsFile) &&
								  !string.IsNullOrWhiteSpace(this.FontName) &&
								  !string.IsNullOrWhiteSpace(this.PrinterDevice) &&
								  !this.HasError;
				}

				return returnValue;
			}
		}

		public bool CanUpdate
		{
			get
			{
				return File.Exists(this.FontSource);
			}
		}

		private bool _changed = true;
		public bool Changed
		{
			get
			{
				return this._changed;
			}
			set
			{
				this.SetProperty(ref this._changed, value);
				this.RefreshCommands();
			}
		}

		private bool _hasError = false;
		public bool HasError
		{
			get
			{
				return this._hasError;
			}
			set
			{
				this.SetProperty(ref this._hasError, value);
				this.RefreshCommands();
			}
		}

		private bool _isIdle = true;
		public bool IsIdle
		{
			get
			{
				return this._isIdle;
			}
			set
			{
				this.SetProperty(ref this._isIdle, value);
				this.RefreshCommands();
			}
		}

		public string SizeDescription
		{
			get
			{
				try
				{
					return this.FontByteLength.Bytes().ToString();
				}
				catch
				{
					return this.FontByteLength.ToString("#,###");
				}
			}
		}

		public async Task SaveAsync()
		{
			try
			{
				if (this.Changed)
				{
					await this.FontService.SaveFontAsync(await this.GetPrinterFontAsync(), this.FontData);
					this.Changed = false;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Properties.Strings.MessageBox_Exception_Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RaisePropertyChanged(nameof(this.CanSave));
			}
		}

		public Task DeleteAsync()
		{
			try
			{
				this.DetailsFile.Delete();
				this.FontDataFile.Delete();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Properties.Strings.MessageBox_Exception_Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RaisePropertyChanged(nameof(this.CanSave));
			}

			return Task.CompletedTask;
		}

		public async Task<IPrinterFont> GetPrinterFontAsync()
		{
			IPrinterFont returnValue = await this.FontService.CreateAsync();

			returnValue.BaseFileName = this.FontDetailsFile;
			returnValue.PrinterDevice = this.PrinterDevice;
			returnValue.FontName = this.FontName;
			returnValue.Chars = this.Chars;
			returnValue.FontByteLength = this.FontByteLength;
			returnValue.FontSource = this.FontSource;

			return returnValue;
		}

		public void RefreshCommands()
		{
			try
			{
				if (!this.Loading)
				{
					//
					// Refresh the state of all of the command buttons.
					//
					this.BrowseCommand.RaiseCanExecuteChanged();
					this.DeleteCommand.RaiseCanExecuteChanged();
					this.SaveCommand.RaiseCanExecuteChanged();
					this.UpdateCommand.RaiseCanExecuteChanged();

					this.EventAggregator.GetEvent<ChangeEvent<FontViewModel>>().Publish(new(ChangeActionType.Refresh, null));
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Properties.Strings.MessageBox_Exception_Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RaisePropertyChanged(nameof(this.CanSave));
				this.RaisePropertyChanged(nameof(this.CanUpdate));
			}
		}

		protected Task DeleteCommandAsync()
		{
			try
			{
				if (MessageBox.Show("Are you sure you want to delete the selected font?", "Delete Font", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
				{
					this.EventAggregator.GetEvent<ChangeEvent<FontViewModel>>().Publish(new ChangeEventArgs<FontViewModel>(ChangeActionType.Delete, this));
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Properties.Strings.MessageBox_Exception_Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}

			return Task.CompletedTask;
		}

		protected async Task BrowseCommandAsync()
		{
			try
			{
				OpenFileDialog dialog = new()
				{
					AddExtension = true,
					CheckFileExists = true,
					CheckPathExists = true,
					DefaultExt = "ttf",
					Filter = "TrueType Font|*.ttf|All Files|*.*",
					FilterIndex = 0,
					Multiselect = false,
					Title = "Select TrueType Font File"
				};

				dialog.ShowDialog();

				if (!string.IsNullOrWhiteSpace(dialog.FileName))
				{
					await this.LoadFontAsync(dialog.FileName);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Properties.Strings.MessageBox_Exception_Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}
		}

		protected Task SaveCommandAsync()
		{
			try
			{
				this.EventAggregator.GetEvent<ChangeEvent<FontViewModel>>().Publish(new ChangeEventArgs<FontViewModel>(ChangeActionType.Save, this));
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Properties.Strings.MessageBox_Exception_Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}

			return Task.CompletedTask;
		}

		protected Task UpdateCommandAsync()
		{
			try
			{
				if (!this.Loading && File.Exists(this.FontSource))
				{
					_ = this.LoadFontAsync(this.FontSource);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Properties.Strings.MessageBox_Exception_Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}

			return Task.CompletedTask;
		}

		protected async Task LoadFontAsync(string fileName)
		{
			try
			{
				(bool result, string fontData, string fontName, int byteLength, string errorMessage) = await this.ConvertFontAsync(fileName, this.Chars);

				if (result)
				{
					this.FontSource = fileName;
					this.FontData = fontData;
					this.FontName = fontName;
					this.FontByteLength = byteLength;
					this.HasError = false;
				}
				else
				{
					this.HasError = true;
					MessageBox.Show(errorMessage, Properties.Strings.MessageBox_FontConversion_Exception_Title, MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Properties.Strings.MessageBox_Exception_Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}
		}

		protected async Task<(bool, string, string, int, string)> ConvertFontAsync(string file, string chars)
		{
			(bool result, string convertedFontData, string fontName, int byteLength, string errorMessage) = (false, null, null, 0, null);

			try
			{
				this.IsIdle = false;

				//
				// Use R for the call.
				//
				string fontPath = "R";

				//
				// Clean the file name.
				//
				string cleanFileName = new(Path.GetFileNameWithoutExtension(file).Where(c => char.IsLetterOrDigit(c)).ToArray());
				fontName = $"{cleanFileName.Limit(16).ToUpperInvariant()}.TTF";

				//
				// Read the font.
				//
				byte[] fontData = File.ReadAllBytes(file);

				using (HttpClient client = new())
				{
					using (MultipartFormDataContent content = [])
					{
						content.Add(new ByteArrayContent(fontData), "file");
						content.Add(new StringContent($"{fontPath}:{fontName}"), "path");

						if (!string.IsNullOrWhiteSpace(chars))
						{
							content.Add(new StringContent(chars), "chars");
						}

						HttpResponseMessage response = await client.PostAsync("https://api.labelary.com/v1/fonts", content);

						string responseData = await response.Content.ReadAsStringAsync();

						if (response.IsSuccessStatusCode)
						{
							//
							// Clean the data.
							//
							convertedFontData = responseData.Replace("^XA", "")
													   .Replace("^XZ", "")
													   .Replace($"~DU{fontPath}:", "");

							//
							// Get the length.
							//
							byteLength = Convert.ToInt32(convertedFontData.Split([',']).Take(2).ToArray()[1]);

							result = true;
						}
						else
						{
							result = false;
							convertedFontData = null;
							fontName = null;
							errorMessage = responseData == "ERROR: null" && !string.IsNullOrEmpty(chars) ? Properties.Strings.FontConversion_Error : responseData;
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Properties.Strings.MessageBox_Exception_Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.IsIdle = true;
			}

			return (result, convertedFontData, fontName, byteLength, errorMessage);
		}
	}
}
