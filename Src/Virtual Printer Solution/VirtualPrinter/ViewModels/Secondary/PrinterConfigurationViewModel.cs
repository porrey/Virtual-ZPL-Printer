using Prism.Mvvm;
using UnitsNet.Units;
using VirtualPrinter.Db.Abstractions;
using VirtualPrinter.HostedService.PrintSystem;

namespace VirtualPrinter.ViewModels
{
	public class PrinterConfigurationViewModel : BindableBase, IPrinterConfiguration
	{
		public PrinterConfigurationViewModel(IPhysicalPrinterFactory physicalPrinterFactory, IPrinterConfiguration item)
		{
			this.PhysicalPrinterFactory = physicalPrinterFactory;

			this.Item = item;
			this.Id = item.Id;
			this.Name = item.Name;
			this.HostAddress = item.HostAddress;
			this.Port = item.Port;
			this.LabelUnit = item.LabelUnit;
			this.LabelWidth = item.LabelWidth;
			this.LabelHeight = item.LabelHeight;
			this.ResolutionInDpmm = item.ResolutionInDpmm;
			this.RotationAngle = item.RotationAngle;
			this.ImagePath = item.ImagePath;
			this.Filters = item.Filters;
			this.PhysicalPrinter = item.PhysicalPrinter;
		}

		protected IPhysicalPrinterFactory PhysicalPrinterFactory { get; set; }

		private IPrinterConfiguration _item = null;
		public IPrinterConfiguration Item
		{
			get
			{
				return this._item;
			}
			set
			{
				this.SetProperty(ref this._item, value);
			}
		}

		private int _id;
		public int Id
		{
			get
			{
				return this._id;
			}
			set
			{
				this.SetProperty(ref this._id, value);
				this.Item.Id = value;
			}
		}

		private string _name;
		public string Name
		{
			get
			{
				return this._name;
			}
			set
			{
				this.SetProperty(ref this._name, value);
				this.Item.Name = value;
			}
		}

		private string _hostAddress;
		public string HostAddress
		{
			get
			{
				return this._hostAddress;
			}
			set
			{
				this.SetProperty(ref this._hostAddress, value);
				this.Item.HostAddress = value;
			}
		}

		private int _port;
		public int Port
		{
			get
			{
				return this._port;
			}
			set
			{
				this.SetProperty(ref this._port, value);
				this.Item.Port = value;
			}
		}

		private int _labelUnit;
		public int LabelUnit
		{
			get
			{
				return this._labelUnit;
			}
			set
			{
				this.SetProperty(ref this._labelUnit, value);
				this.Item.LabelUnit = value;
			}
		}

		private double labelWidth;
		public double LabelWidth
		{
			get
			{
				return this.labelWidth;
			}
			set
			{
				this.SetProperty(ref this.labelWidth, value);
				this.Item.LabelWidth = value;
			}
		}

		private double _labelHeight;
		public double LabelHeight
		{
			get
			{
				return this._labelHeight;
			}
			set
			{
				this.SetProperty(ref this._labelHeight, value);
				this.Item.LabelHeight = value;
			}
		}

		private int _resolutionInDpmm;
		public int ResolutionInDpmm
		{
			get
			{
				return this._resolutionInDpmm;
			}
			set
			{
				this.SetProperty(ref this._resolutionInDpmm, value);
				this.Item.ResolutionInDpmm = value;
			}
		}

		private int _rotationAngle;
		public int RotationAngle
		{
			get
			{
				return this._rotationAngle;
			}
			set
			{
				this.SetProperty(ref this._rotationAngle, value);
				this.Item.RotationAngle = value;
			}
		}

		private string _imagePath;
		public string ImagePath
		{
			get
			{
				return this._imagePath;
			}
			set
			{
				this.SetProperty(ref this._imagePath, value);
				this.Item.ImagePath = value;
			}
		}

		private string _filters;
		public string Filters
		{
			get
			{
				return this._filters;
			}
			set
			{
				this.SetProperty(ref this._filters, value);
				this.Item.Filters = value;
			}
		}

		private string _physicalPrinter;
		public string PhysicalPrinter
		{
			get
			{
				return this._physicalPrinter ?? "{}";
			}
			set
			{
				this.SetProperty(ref this._physicalPrinter, value);
				this.Item.PhysicalPrinter = value;
			}
		}

		private IPhysicalPrinter _physicalPrinterInstance = null;
		public IPhysicalPrinter PhysicalPrinterInstance
		{
			get
			{
				this._physicalPrinterInstance ??= this.PhysicalPrinterFactory.Deserialize(this.PhysicalPrinter);
				return this._physicalPrinterInstance;
			}
		}

		public string PhysicalPrinterDescription => $"{Properties.Strings.Printer_Configuration_Printing} {(this.PhysicalPrinterInstance.Enabled ? Properties.Strings.Printer_Configuration_Enabled : Properties.Strings.Printer_Configuration_Disabled)}{(this.PhysicalPrinterInstance.Enabled ? $": {this.PhysicalPrinterInstance.PrinterName}" : "")}";

		public string IdSummary => $"{Properties.Strings.Printer_Configuration_Id}: {this.Id}";
		public string HostSummary => $"{Properties.Strings.Printer_Configuration_Host}: {this.HostAddress}:{this.Port}";
		public string SizeSummary => $"{Properties.Strings.Printer_Configuration_Size}: {this.LabelWidth} {this.Unit} {Properties.Strings.PrinterConfigurationViewModel_By} {this.LabelHeight} {this.Unit}";
		public string ResolutionSummary => $"{Properties.Strings.Printer_Configuration_Resolution}: {this.ResolutionInDpmm} {Properties.Strings.dpmm}";
		public string RotationSummary => $"{Properties.Strings.Printer_Configuration_Rotation}: {this.RotationAngle}˚";
		public string Description => $"{this.Name} [{this.HostSummary}, {this.SizeSummary}, {this.ResolutionSummary}, {this.RotationSummary}, {this.PhysicalPrinterDescription}]";
		public string Unit => $"{(LengthUnit)this.LabelUnit}".ToLower();

		public object Clone() => throw new System.NotImplementedException();
	}
}
