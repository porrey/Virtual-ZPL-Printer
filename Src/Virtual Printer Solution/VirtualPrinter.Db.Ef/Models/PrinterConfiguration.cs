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
