using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VirtualZplPrinter.Converters
{
	public class NotBooleanToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			Visibility returnValue = Visibility.Collapsed;

			if (value is bool flag)
			{
				returnValue = flag ? Visibility.Collapsed : Visibility.Visible;
			}

			return returnValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool returnValue = false;

			if (value is Visibility flag)
			{
				returnValue = flag == Visibility.Visible ? false : true;
			}

			return returnValue;
		}
	}
}
