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
	public interface IPhysicalPrinter
	{
		double BottomMargin { get; set; }
		bool Enabled { get; set; }
		bool HorizontalAlignCenter { get; set; }
		bool HorizontalAlignLeft { get; set; }
		bool HorizontalAlignRight { get; set; }
		double LeftMargin { get; set; }
		string PrinterName { get; set; }
		double RightMargin { get; set; }
		double TopMargin { get; set; }
		bool VerticalAlignBottom { get; set; }
		bool VerticalAlignMiddle { get; set; }
		bool VerticalAlignTop { get; set; }
	}
}