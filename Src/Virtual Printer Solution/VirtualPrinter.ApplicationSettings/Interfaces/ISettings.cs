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
namespace VirtualPrinter.ApplicationSettings
{
	public interface ISettings
	{
		void Save();
		DirectoryInfo RootFolder { get; set; }
		double Width { get; set; }
		double Height { get; set; }
		double Top { get; set; }
		double Left { get; set; }
		int WindowState { get; set; }
		bool Initialized { get; set; }
		bool AutoStart { get; set; }
		int PrinterConfiguration { get; set; }
		string LabelTemplate { get; set; }
		double SendTestLabelLeft { get; set; }
		double SendTestLabelTop { get; set; }
		double SendTestLabelWidth { get; set; }
		double SendTestLabelHeight { get; set; }
		int ReceiveTimeout { get; set; }
		int SendTimeout { get; set; }
		bool NoDelay { get; set; }
		bool Linger { get; set; }
		int LingerTime { get; set; }
		int ReceiveBufferSize { get; set; }
		int SendBufferSize { get; set; }
		string ReceivedDataEncoding { get; set; }
		string ApiUrl { get; set; }
		string ApiMethod { get; set; }
		bool ApiLinting { get; set; }
	}
}
