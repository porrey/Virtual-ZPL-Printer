using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Diamond.Core.Repository;
using UnitsNet.Units;
using VirtualPrinter.Db.Abstractions;

namespace VirtualPrinter.Db.Ef
{
	[Table("PrinterConfiguration")]
	public class PrinterConfiguration : Entity, IPrinterConfiguration
	{
		[Column("PrinterConfigurationId")]
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public string Name { get; set; }
		public string HostAddress { get; set; }
		public int Port { get; set; }
		public int LabelUnit { get; set; }
		public double LabelWidth { get; set; }
		public double LabelHeight { get; set; }
		public int ResolutionInDpmm { get; set; }
		public int RotationAngle { get; set; }
		public string ImagePath { get; set; }

		[NotMapped]
		public string IdSummary => $"ID: {this.Id}";

		[NotMapped]
		public string HostSummary => $"Host: {this.HostAddress}:{this.Port}";

		[NotMapped]
		public string SizeSummary => $"Size: {this.LabelWidth} {this.Unit} by {this.LabelHeight} {this.Unit}";

		[NotMapped]
		public string ResolutionSummary => $"Resolution: {this.ResolutionInDpmm} dpmm";

		[NotMapped]
		public string RotationSummary => $"Rotation: {this.RotationAngle}˚";

		[NotMapped]
		public string Description => $"{this.Name} [{this.HostSummary}, {this.SizeSummary}, {this.ResolutionSummary}, {this.RotationSummary}]";

		[NotMapped]
		public string Unit => $"{(LengthUnit)this.LabelUnit}".ToLower();
	}
}
