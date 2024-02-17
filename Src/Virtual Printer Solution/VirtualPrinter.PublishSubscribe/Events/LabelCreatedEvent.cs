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
using ImageCache.Abstractions;
using Labelary.Abstractions;
using Prism.Events;
using VirtualPrinter.Db.Abstractions;

namespace VirtualPrinter.PublishSubscribe
{
	public class LabelCreatedEvent : PubSubEvent<LabelCreatedEventArgs>
	{
	}

	public class LabelCreatedEventArgs : EventArgs
	{
		public IPrinterConfiguration PrinterConfiguration { get; set; }
		public PrintRequestEventArgs PrintRequest { get; set; }
		public IStoredImage Label { get; set; }
		public bool Result { get; set; }
		public string Message { get; set; }
		public IEnumerable<Warning> Warnings { get; set; }
	}
}