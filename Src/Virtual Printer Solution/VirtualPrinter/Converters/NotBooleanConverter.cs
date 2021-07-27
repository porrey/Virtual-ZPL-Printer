using System;
using System.Globalization;
using System.Windows.Data;

namespace VirtualZplPrinter.Converters
{
	public class NotBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool returnValue = false;

			if (value is bool flag)
			{
				returnValue = !flag;
			}

			return returnValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool returnValue = false;

			if (value is bool flag)
			{
				returnValue = !flag;
			}

			return returnValue;
		}
	}
}
