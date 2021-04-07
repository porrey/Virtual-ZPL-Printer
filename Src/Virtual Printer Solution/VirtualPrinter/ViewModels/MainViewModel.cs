using System.Collections.ObjectModel;
using VirtualPrinter.Models;

namespace VirtualPrinter.ViewModels
{
	public class MainViewModel
	{
		public MainViewModel()
		{
			this.Resolutions.Add(new Resolution() { Dpmm = "6dpmm", Dpi = "152 dpi" });
			this.Resolutions.Add(new Resolution() { Dpmm = "8dpmm", Dpi = "203 dpi" });
			this.Resolutions.Add(new Resolution() { Dpmm = "12dpmm", Dpi = "305 dpi" });
			this.Resolutions.Add(new Resolution() { Dpmm = "24dpmm", Dpi = "610 dpi" });
		}

		public ObservableCollection<Resolution> Resolutions { get; } = new ObservableCollection<Resolution>();

		private Resolution _selectedResolution = null;
		public Resolution SelectedResolution
		{
			get
			{
				return _selectedResolution;
			}
			set
			{
				_selectedResolution = value;
			}
		}
	}
}
