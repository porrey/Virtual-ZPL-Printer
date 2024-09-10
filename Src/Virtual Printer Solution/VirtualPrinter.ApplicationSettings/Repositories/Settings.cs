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
	internal class Settings : ISettings
	{
		public void Save()
		{
			Properties.Settings.Default.Save();
		}

		public DirectoryInfo RootFolder { get => new($@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\Virtual ZPL Printer"); set => Properties.Settings.Default.RootFolder = value.FullName; }
		public double Width { get => Properties.Settings.Default.Width; set => Properties.Settings.Default.Width = value; }
		public double Height { get => Properties.Settings.Default.Height; set => Properties.Settings.Default.Height = value; }
		public double Top { get => Properties.Settings.Default.Top; set => Properties.Settings.Default.Top = value; }
		public double Left { get => Properties.Settings.Default.Left; set => Properties.Settings.Default.Left = value; }
		public int WindowState { get => Properties.Settings.Default.WindowState; set => Properties.Settings.Default.WindowState = value; }
		public bool Initialized { get => Properties.Settings.Default.Initialized; set => Properties.Settings.Default.Initialized = value; }
		public bool AutoStart { get => Properties.Settings.Default.AutoStart; set => Properties.Settings.Default.AutoStart = value; }
		public int PrinterConfiguration { get => Properties.Settings.Default.PrinterConfiguration; set => Properties.Settings.Default.PrinterConfiguration = value; }
		public string LabelTemplate { get => Properties.Settings.Default.LabelTemplate; set => Properties.Settings.Default.LabelTemplate = value; }
		public double SendTestLabelLeft { get => Properties.Settings.Default.SendTestLabelLeft; set => Properties.Settings.Default.SendTestLabelLeft = value; }
		public double SendTestLabelTop { get => Properties.Settings.Default.SendTestLabelTop; set => Properties.Settings.Default.SendTestLabelTop = value; }
		public double SendTestLabelWidth { get => Properties.Settings.Default.SendTestLabelWidth; set => Properties.Settings.Default.SendTestLabelWidth = value; }
		public double SendTestLabelHeight { get => Properties.Settings.Default.SendTestLabelHeight; set => Properties.Settings.Default.SendTestLabelHeight = value; }
		public int ReceiveTimeout { get => Properties.Settings.Default.ReceiveTimeout; set => Properties.Settings.Default.ReceiveTimeout = value; }
		public int SendTimeout { get => Properties.Settings.Default.ReceiveTimeout; set => Properties.Settings.Default.ReceiveTimeout = value; }
		public bool NoDelay { get => Properties.Settings.Default.NoDelay; set => Properties.Settings.Default.NoDelay = value; }
		public bool Linger { get => Properties.Settings.Default.Linger; set => Properties.Settings.Default.Linger = value; }
		public int LingerTime { get => Properties.Settings.Default.LingerTime; set => Properties.Settings.Default.LingerTime = value; }
		public int ReceiveBufferSize { get => Properties.Settings.Default.ReceiveBufferSize; set => Properties.Settings.Default.ReceiveBufferSize = value; }
		public int SendBufferSize { get => Properties.Settings.Default.SendBufferSize; set => Properties.Settings.Default.SendBufferSize = value; }
		public string ReceivedDataEncoding { get => Properties.Settings.Default.ReceivedDataEncoding; set => Properties.Settings.Default.ReceivedDataEncoding = value; }
		public string ApiUrl { get => Properties.Settings.Default.ApiUrl; set => Properties.Settings.Default.ApiUrl = value; }
		public string ApiMethod { get => Properties.Settings.Default.ApiMethod; set => Properties.Settings.Default.ApiMethod = value; }
		public bool ApiLinting { get => Properties.Settings.Default.ApiLinting; set => Properties.Settings.Default.ApiLinting = value; }
		public int MaximumLabels { get => Properties.Settings.Default.MaximumLables; set => Properties.Settings.Default.MaximumLables = value; }
		public int MaximumWaitTime { get => Properties.Settings.Default.MaximumWaitTime; set => Properties.Settings.Default.MaximumWaitTime = value; }
	}
}
