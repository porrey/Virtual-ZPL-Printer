using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Labelary.Abstractions;

namespace Labelary.Service
{
	public static class ZplDecorator
	{
		public static string Filter(this string zpl, IEnumerable<ILabelFilter> filters)
		{
			string returnValue = zpl;

			foreach (ILabelFilter filter in filters.OrderBy(t => t.Priority))
			{
				string replace = filter.Replace == null ? string.Empty : filter.Replace;

				if (filter.TreatAsRegularExpression)
				{
					try
					{
						returnValue = Regex.Replace(returnValue, filter.Find, replace);
					}
					catch { }
				}
				else
				{
					try
					{
						returnValue = returnValue.Replace(filter.Find, replace);
					}
					catch { }
				}
			}

			return returnValue;
		}
	}
}
