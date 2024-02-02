using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Diamond.Core.Repository;
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
		public string Filters { get; set; }
		public string PhysicalPrinter { get; set; }
	}
}
