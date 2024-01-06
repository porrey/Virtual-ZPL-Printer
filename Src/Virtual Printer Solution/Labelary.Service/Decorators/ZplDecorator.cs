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
				string replace = filter.Replace ?? string.Empty;

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

		public static string GetParameterValue(this string zpl, string parameterName, string defaultValue)
		{
			string returnValue = defaultValue;

			//
			// Get the comment lines from the ZPL.
			//
			string line = (from tbl in zpl.Split(new char[] { '\r', '\n' }, StringSplitOptions.TrimEntries & StringSplitOptions.RemoveEmptyEntries)
						   where tbl.StartsWith("^FX") &&
						   tbl.ToLower().Contains(parameterName.ToLower())
						   select tbl).FirstOrDefault();

			if (line != null)
			{
				string[] parts = line.Split(new char[] { ':' }, StringSplitOptions.TrimEntries & StringSplitOptions.RemoveEmptyEntries);

				if (parts.Length == 2)
				{
					returnValue = parts[1];
				}
			}

			return returnValue;
		}
	}
}
