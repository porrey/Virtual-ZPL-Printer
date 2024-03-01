using Prism.Mvvm;

namespace VirtualPrinter.ViewModels
{
	public class TextEncodingViewModel : BindableBase
	{
		private string _displayName = string.Empty;
		public string DisplayName
		{
			get
			{
				return this._displayName;
			}
			set
			{
				this.SetProperty(ref this._displayName, value);
			}
		}

		private string _value = string.Empty;
		public string Value
		{
			get
			{
				return this._value;
			}
			set
			{
				this.SetProperty(ref this._value, value);
			}
		}
	}
}
