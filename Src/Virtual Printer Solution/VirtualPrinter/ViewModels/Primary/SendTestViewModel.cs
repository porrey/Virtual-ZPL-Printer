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
using System.Net;
using Diamond.Core.Repository;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;
using VirtualPrinter.ApplicationSettings;
using VirtualPrinter.Db.Abstractions;
using VirtualPrinter.TemplateManager;
using VirtualPrinter.TestClient;

namespace VirtualPrinter.ViewModels
{
	public class SendTestViewModel : BindableBase
	{
		public SendTestViewModel(ILogger<SendTestViewModel> logger, IRepositoryFactory repositoryFactory, ISettings settings, ITemplateFactory templateFactory, IZplClient zplClient)
		{
			this.Logger = logger;
			this.RepositoryFactory = repositoryFactory;
			this.Settings = settings;
			this.TemplateFactory = templateFactory;
			this.ZplClient = zplClient;

			this.SendCommand = new(() => _ = this.SendCommandAsync(), () => this.SelectedLabelTemplate != null);
		}

		protected ILogger<SendTestViewModel> Logger { get; set; }
		protected IRepositoryFactory RepositoryFactory { get; set; }
		protected ISettings Settings { get; set; }
		protected ITemplateFactory TemplateFactory { get; set; }
		protected IZplClient ZplClient { get; set; }

		public DelegateCommand SendCommand { get; set; }
		public ObservableCollection<ILabelTemplate> LabelTemplates { get; } = [];

		private ILabelTemplate _selectedLabelTemplate = null;
		public ILabelTemplate SelectedLabelTemplate
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
					this.Settings.LabelTemplate = value.Name;
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
			await this.LoadLabelTemplatesAsync();
		}

		protected async Task LoadLabelTemplatesAsync()
		{
			//
			// Clear the list.
			//
			this.LabelTemplates.Clear();

			//
			// Get the template repository.
			//
			IReadOnlyRepository<ILabelTemplate> repository = await this.RepositoryFactory.GetReadOnlyAsync<ILabelTemplate>();

			//
			// Get all of the templates.
			//
			IEnumerable<ILabelTemplate> templates = await repository.GetAllAsync();

			//
			// Load each template.
			//
			foreach (ILabelTemplate template in templates)
			{
				this.LabelTemplates.Add(template);
			}

			//
			// Select a label.
			//
			this.SelectedLabelTemplate = this.LabelTemplates.Where(t => t.Name == this.Settings.LabelTemplate).SingleOrDefault();
			this.SelectedLabelTemplate ??= this.LabelTemplates.FirstOrDefault();
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
			//
			// Get the IP address to send the label to.
			//
			IPAddress ip = this.SelectedPrinterConfiguration.HostAddress == IPAddress.Any.ToString() ? IPAddress.Loopback : IPAddress.Parse(this.SelectedPrinterConfiguration.HostAddress);

			//
			// Apply template field values.
			//
			ILabelTemplate clonedTemplate = (ILabelTemplate)this.SelectedLabelTemplate.Clone();
			clonedTemplate.Zpl = this.Zpl;
			string zpl = await this.TemplateFactory.CreateZplAsync(clonedTemplate);

			//
			// Send the label.
			//
			_ = await this.ZplClient.SendStringAsync(ip, this.SelectedPrinterConfiguration.Port, zpl);
		}
	}
}
