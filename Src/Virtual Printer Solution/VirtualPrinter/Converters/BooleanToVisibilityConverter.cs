using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VirtualZplPrinter.Converters
{
	public class BooleanToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			Visibility returnValue = Visibility.Collapsed;

			if (value is bool flag)
			{
				returnValue = flag ? Visibility.Visible : Visibility.Collapsed;
			}

			return returnValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool returnValue = false;

			if (value is Visibility flag)
			{
				returnValue = flag == Visibility.Visible ? true : false;
			}

			return returnValue;
		}
	}
}
