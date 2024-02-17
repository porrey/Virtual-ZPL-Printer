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
using System;
using System.Globalization;
using System.Net;
using System.Windows.Data;
using VirtualPrinter.Repository.HostAddresses;

namespace VirtualPrinter.Converters
{
	public class HostAddressToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string returnValue = "Unknown";

			if (value is IHostAddress hostAddress)
			{
				if (hostAddress.IpAddress == IPAddress.Any)
				{
					returnValue = "Any";
				}
				else if (hostAddress.IpAddress == IPAddress.Loopback)
				{
					returnValue = "Loopback";
				}
				else
				{
					returnValue = hostAddress.Address;
				}
			}

			return returnValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
