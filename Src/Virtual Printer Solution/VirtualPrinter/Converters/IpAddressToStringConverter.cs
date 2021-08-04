using System;
using System.Globalization;
using System.Net;
using System.Windows.Data;

namespace VirtualZplPrinter.Converters
{
	public class IpAddressToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string returnValue = null;

			if (value is IPAddress address)
			{
				if (address == IPAddress.Any)
				{
					returnValue = "Any";
				}
				else
				{
					returnValue = address.ToString();
				}
			}
			else
			{
				returnValue = "Unknown";
			}

			return returnValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
