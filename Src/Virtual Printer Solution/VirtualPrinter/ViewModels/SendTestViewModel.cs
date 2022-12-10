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
			this.SendCommand = new DelegateCommand(() => _ = this.SendCommandAsync(), () => this.SelectedLabelTemplate != null);
		}

		public DelegateCommand SendCommand { get; set; }
		public ObservableCollection<LabelTemplate> LabelTemplates { get; } = new ObservableCollection<LabelTemplate>();

		private LabelTemplate _selectedLabelTemplate = null;
		public LabelTemplate SelectedLabelTemplate
		{
			get
			{
				return _selectedLabelTemplate;
			}
			set
			{
				this.SetProperty(ref _selectedLabelTemplate, value);
				this.RefreshCommands();

				if (value != null)
				{
					Properties.Settings.Default.LabelTemplate = value.Name;
				}
			}
		}

		private IPrinterConfiguration _printerConfiguration = null;
		public IPrinterConfiguration SelectedPrinterConfiguration
		{
			get
			{
				return _printerConfiguration;
			}
			set
			{
				this.SetProperty(ref _printerConfiguration, value);
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
			_ = await TestClient.SendStringAsync(ip, this.SelectedPrinterConfiguration.Port, this.SelectedLabelTemplate.ApplyFieldValues());
		}
	}
}
