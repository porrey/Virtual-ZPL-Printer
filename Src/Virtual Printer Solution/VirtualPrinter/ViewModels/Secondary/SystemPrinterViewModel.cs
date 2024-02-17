using Prism.Mvvm;

namespace VirtualPrinter.Models
{
	public class SystemPrinterViewModel : BindableBase
	{
		private string _name = null;
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				this.SetProperty(ref this._name, value);
			}
		}
	}
}