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
using Diamond.Core.Repository;

namespace VirtualPrinter.Db.Abstractions
{
	public interface IPrinterConfiguration : IEntity<int>
	{
		string Name { get; set; }
		string HostAddress { get; set; }
		int Port { get; set; }
		int LabelUnit { get; set; }
		double LabelWidth { get; set; }
		double LabelHeight { get; set; }
		int ResolutionInDpmm { get; set; }
		int RotationAngle { get; set; }
		string Filters { get; set; }
		string PhysicalPrinter { get; set; }
		string ImagePath { get; set; }
	}
}