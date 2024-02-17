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
using System.Drawing.Printing;
using System.Windows;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using VirtualPrinter.HostedService.PrintSystem;
using VirtualPrinter.Models;

namespace VirtualPrinter.ViewModels
{
	public class EditPrinterViewModel : BindableBase
	{
		public EditPrinterViewModel(ILogger<EditPrinterViewModel> logger, IEventAggregator eventAggregator, IPhysicalPrinterFactory physicalPrinterFactory)
			: base()
		{
			this.Logger = logger;
			this.EventAggregator = eventAggregator;
			this.PhysicalPrinterFactory = physicalPrinterFactory;

			this.OkCommand = new(async () => await this.OkCommandAsync(), () => true);
			this.CancelCommand = new(async () => await this.CancelCommandAsync(), () => true);
		}

		protected ILogger<EditPrinterViewModel> Logger { get; set; }
		protected IEventAggregator EventAggregator { get; set; }
		protected IPhysicalPrinterFactory PhysicalPrinterFactory { get; set; }

		public DelegateCommand OkCommand { get; set; }
		public DelegateCommand CancelCommand { get; set; }

		public ObservableCollection<SystemPrinterViewModel> SystemPrinters { get; } = [];

		private bool _updated = false;
		public bool Updated
		{
			get
			{
				return this._updated;
			}
			set
			{
				this.SetProperty(ref this._updated, value);
				this.RefreshCommands();

				if (this.Updated)
				{
					this.ButtonName = "Ok";
				}
				else
				{
					this.ButtonName = "Close";
				}
			}
		}

		private bool _enabled = false;
		public bool Enabled
		{
			get
			{
				return this._enabled;
			}
			set
			{
				this.SetProperty(ref this._enabled, value);
				this.RefreshCommands();
				this.Updated = true;
			}
		}

		private SystemPrinterViewModel _selectedSystemPrinter = null;
		public SystemPrinterViewModel SelectedSystemPrinter
		{
			get
			{
				return this._selectedSystemPrinter;
			}
			set
			{
				this.SetProperty(ref this._selectedSystemPrinter, value);
				this.RefreshCommands();
				this.Updated = true;
			}
		}

		private bool _verticalAlignLeft = true;
		public bool VerticalAlignLeft
		{
			get
			{
				return this._verticalAlignLeft;
			}
			set
			{
				this.SetProperty(ref this._verticalAlignLeft, value);
				this.RefreshCommands();
				this.Updated = true;
			}
		}

		private bool _verticalAlignCenter = false;
		public bool VerticalAlignCenter
		{
			get
			{
				return this._verticalAlignCenter;
			}
			set
			{
				this.SetProperty(ref this._verticalAlignCenter, value);
				this.RefreshCommands();
				this.Updated = true;
			}
		}

		private bool _verticalAlignRight = false;
		public bool VerticalAlignRight
		{
			get
			{
				return this._verticalAlignRight;
			}
			set
			{
				this.SetProperty(ref this._verticalAlignRight, value);
				this.RefreshCommands();
				this.Updated = true;
			}
		}

		private bool _horizontalAlignLeft = true;
		public bool HorizontalAlignLeft
		{
			get
			{
				return this._horizontalAlignLeft;
			}
			set
			{
				this.SetProperty(ref this._horizontalAlignLeft, value);
				this.RefreshCommands();
				this.Updated = true;
			}
		}

		private bool _horizontalAlignCenter = false;
		public bool HorizontalAlignCenter
		{
			get
			{
				return this._horizontalAlignCenter;
			}
			set
			{
				this.SetProperty(ref this._horizontalAlignCenter, value);
				this.RefreshCommands();
				this.Updated = true;
			}
		}

		private bool _horizontalAlignRight = false;
		public bool HorizontalAlignRight
		{
			get
			{
				return this._horizontalAlignRight;
			}
			set
			{
				this.SetProperty(ref this._horizontalAlignRight, value);
				this.RefreshCommands();
				this.Updated = true;
			}
		}

		private double _leftMargin = 0;
		public double LeftMargin
		{
			get
			{
				return this._leftMargin;
			}
			set
			{
				this.SetProperty(ref this._leftMargin, value);
				this.RefreshCommands();
				this.Updated = true;
			}
		}

		private double _rightMargin = 0;
		public double RightMargin
		{
			get
			{
				return this._rightMargin;
			}
			set
			{
				this.SetProperty(ref this._rightMargin, value);
				this.RefreshCommands();
				this.Updated = true;
			}
		}

		private double _topMargin = 0;
		public double TopMargin
		{
			get
			{
				return this._topMargin;
			}
			set
			{
				this.SetProperty(ref this._topMargin, value);
				this.RefreshCommands();
				this.Updated = true;
			}
		}

		private double _bottomMargin = 0;
		public double BottomMargin
		{
			get
			{
				return this._bottomMargin;
			}
			set
			{
				this.SetProperty(ref this._bottomMargin, value);
				this.RefreshCommands();
				this.Updated = true;
			}
		}

		private string _buttonName = "Close";
		public string ButtonName
		{
			get
			{
				return this._buttonName;
			}
			set
			{
				this.SetProperty(ref this._buttonName, value);
			}
		}

		public IPhysicalPrinter PhysicalPrinter
		{
			get
			{
				IPhysicalPrinter returnValue = this.PhysicalPrinterFactory.Create();

				returnValue.Enabled = this.Enabled;
				returnValue.PrinterName = this.SelectedSystemPrinter.Name;
				returnValue.VerticalAlignTop = this.VerticalAlignLeft;
				returnValue.VerticalAlignMiddle = this.VerticalAlignCenter;
				returnValue.VerticalAlignBottom = this.VerticalAlignRight;
				returnValue.HorizontalAlignLeft = this.HorizontalAlignLeft;
				returnValue.HorizontalAlignCenter = this.HorizontalAlignCenter;
				returnValue.HorizontalAlignRight = this.HorizontalAlignRight;
				returnValue.LeftMargin = this.LeftMargin;
				returnValue.RightMargin = this.RightMargin;
				returnValue.TopMargin = this.TopMargin;
				returnValue.BottomMargin = this.BottomMargin;

				return returnValue;
			}
			set
			{
				this.Enabled = value.Enabled;
				this.SelectedSystemPrinter = this.SystemPrinters.Where(t => t.Name == value.PrinterName).FirstOrDefault();
				this.VerticalAlignLeft = value.VerticalAlignTop;
				this.VerticalAlignCenter = value.VerticalAlignMiddle;
				this.VerticalAlignRight = value.VerticalAlignBottom;
				this.HorizontalAlignLeft = value.HorizontalAlignLeft;
				this.HorizontalAlignCenter = value.HorizontalAlignCenter;
				this.HorizontalAlignRight = value.HorizontalAlignRight;
				this.LeftMargin = value.LeftMargin;
				this.RightMargin = value.RightMargin;
				this.TopMargin = value.TopMargin;
				this.BottomMargin = value.BottomMargin;
				this.Updated = false;
			}
		}

		public Task InitializeAsync()
		{
			try
			{
				this.LoadPrinters();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}

			return Task.CompletedTask;
		}

		protected void LoadPrinters()
		{
			try
			{
				//
				// Load the system printers.
				//
				this.SystemPrinters.Clear();

				foreach (object printer in PrinterSettings.InstalledPrinters)
				{
					this.SystemPrinters.Add(new SystemPrinterViewModel() { Name = Convert.ToString(printer) });
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				this.RefreshCommands();
			}
		}

		protected Task OkCommandAsync()
		{
			return Task.CompletedTask;
		}

		protected Task CancelCommandAsync()
		{
			this.Updated = false;
			return Task.CompletedTask;
		}

		public void RefreshCommands()
		{
			//
			// Refresh the state of all of the command buttons.
			//
			this.OkCommand.RaiseCanExecuteChanged();
			this.CancelCommand.RaiseCanExecuteChanged();
		}
	}
}
