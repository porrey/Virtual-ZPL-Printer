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
using System.Data;
using Labelary.Abstractions;
using UnitsNet.Units;
using VirtualPrinter.Db.Abstractions;

namespace VirtualPrinter.Repository.LabelParameters
{
	public class DefaultPrinterConfiguration : IPrinterConfiguration
	{
		public int Id { get => 0; set => throw new ReadOnlyException(); }
		public string Name { get => null; set => throw new ReadOnlyException(); }
		public string HostAddress { get => "0.0.0.0"; set => throw new ReadOnlyException(); }
		public int Port { get => 9100; set => throw new ReadOnlyException(); }
		public int LabelUnit { get => (int)LengthUnit.Inch; set => throw new ReadOnlyException(); }
		public double LabelWidth { get => 4; set => throw new ReadOnlyException(); }
		public double LabelHeight { get => 6; set => throw new ReadOnlyException(); }
		public int ResolutionInDpmm { get => 8; set => throw new ReadOnlyException(); }
		public int RotationAngle { get => 0; set => throw new ReadOnlyException(); }
		public string ImagePath { get => FileLocations.ImageCache.FullName; set => throw new ReadOnlyException(); }
		public string Filters { get => null; set => throw new ReadOnlyException(); }
		public string PhysicalPrinter { get => null; set => throw new ReadOnlyException(); }

		public object Clone() => throw new NotImplementedException();

		public static DefaultPrinterConfiguration Instance { get; } = new DefaultPrinterConfiguration();
	}
}
