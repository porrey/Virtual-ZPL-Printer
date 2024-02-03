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
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Labelary.Abstractions;
using Prism.Commands;
using Prism.Mvvm;
using VirtualPrinter.Db.Abstractions;
using VirtualPrinter.Models;

namespace VirtualPrinter.ViewModels
{
	public class SendTestViewModel : BindableBase
	{
		public SendTestViewModel()
		{
			this.SendCommand = new(() => _ = this.SendCommandAsync(), () => this.SelectedLabelTemplate != null);
		}

		public DelegateCommand SendCommand { get; set; }
		public ObservableCollection<LabelTemplate> LabelTemplates { get; } = new ObservableCollection<LabelTemplate>();

		private LabelTemplate _selectedLabelTemplate = null;
		public LabelTemplate SelectedLabelTemplate
		{
			get
			{
				return this._selectedLabelTemplate;
			}
			set
			{
				this.SetProperty(ref this._selectedLabelTemplate, value);
				this.RefreshCommands();

				if (value != null)
				{
					Properties.Settings.Default.LabelTemplate = value.Name;
					this.Zpl = this.SelectedLabelTemplate.Zpl;
				}
			}
		}

		private IPrinterConfiguration _printerConfiguration = null;
		public IPrinterConfiguration SelectedPrinterConfiguration
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

		private string _zpl = null;
		public string Zpl
		{
			get
			{
				return this._zpl;
			}
			set
			{
				this.SetProperty(ref this._zpl, value);
				this.RefreshCommands();
			}
		}

		public async Task InitializeAsync()
		{
			await this.LoadLabelTemplates();
		}

		protected Task LoadLabelTemplates()
		{
			//
			// Clear the list.
			//
			this.LabelTemplates.Clear();

			//
			// Get the files
			//
			FileLocations.LabelTemplates.Create();
			FileInfo[] files = FileLocations.LabelTemplates.GetFiles("*.zpl");

			//
			// Load each template.
			//
			foreach (FileInfo file in files)
			{
				this.LabelTemplates.Add(new LabelTemplate() { TemplateFile = file });
			}

			//
			// Select a label.
			//
			this.SelectedLabelTemplate = this.LabelTemplates.Where(t => t.Name == Properties.Settings.Default.LabelTemplate).SingleOrDefault();

			if (this.SelectedLabelTemplate == null)
			{
				this.SelectedLabelTemplate = this.LabelTemplates.FirstOrDefault();
			}

			return Task.CompletedTask;
		}

		public void RefreshCommands()
		{
			//
			// Refresh the state of all of the command buttons.
			//
			this.SendCommand.RaiseCanExecuteChanged();
		}

		protected async Task SendCommandAsync()
		{
			IPAddress ip = this.SelectedPrinterConfiguration.HostAddress == IPAddress.Any.ToString() ? IPAddress.Loopback : IPAddress.Parse(this.SelectedPrinterConfiguration.HostAddress);
			_ = await TestClient.SendStringAsync(ip, this.SelectedPrinterConfiguration.Port, this.Zpl.ApplyFieldValues());
		}
	}
}
