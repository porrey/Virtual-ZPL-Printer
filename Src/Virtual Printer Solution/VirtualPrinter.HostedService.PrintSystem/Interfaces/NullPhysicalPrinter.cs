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
namespace VirtualPrinter.HostedService.PrintSystem
{
	public class NullPhysicalPrinter : IPhysicalPrinter
	{
		public double BottomMargin { get; set; } = 0;
		public bool Enabled { get; set; } = false;
		public bool HorizontalAlignCenter { get; set; } = false;
		public bool HorizontalAlignLeft { get; set; } = false;
		public bool HorizontalAlignRight { get; set; } = false;
		public double LeftMargin { get; set; } = 0;
		public string PrinterName { get; set; } = "Null Printer";
		public double RightMargin { get; set; } = 0;
		public double TopMargin { get; set; } = 0;
		public bool VerticalAlignBottom { get; set; } = false;
		public bool VerticalAlignMiddle { get; set; } = false;
		public bool VerticalAlignTop { get; set; } = false;
	}
}