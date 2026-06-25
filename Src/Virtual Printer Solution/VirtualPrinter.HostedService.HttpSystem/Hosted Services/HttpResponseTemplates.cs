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
using System.Reflection;
using System.Text;

namespace VirtualPrinter.HostedService.HttpSystem
{
	internal static class HttpResponseTemplates
	{
		private static readonly Assembly Asm = Assembly.GetExecutingAssembly();

		private static string Load(string resourceName)
		{
			string fullName = $"VirtualPrinter.HostedService.HttpSystem.Resources.{resourceName}";
			using Stream stream = Asm.GetManifestResourceStream(fullName)
				?? throw new InvalidOperationException($"Embedded resource '{fullName}' not found.");
			using var reader = new StreamReader(stream, Encoding.UTF8);
			return reader.ReadToEnd();
		}

		private static string Fill(string template, params (string Key, string Value)[] replacements)
		{
			foreach ((string key, string value) in replacements)
				template = template.Replace($"{{{{{key}}}}}", value, StringComparison.Ordinal);
			return template;
		}

		public static string BuildIndexPage(string statusBanner, string formatTable, string grfTable)
			=> Fill(Load("IndexPage.html"),
				("STATUS_BANNER", statusBanner),
				("FORMAT_TABLE",  formatTable),
				("GRF_TABLE",     grfTable));

		public static string BuildZplEditorPage(string pageTitle, string displayName, string statusBanner, string zplContentEncoded, string jsVars)
			=> Fill(Load("ZplEditorPage.html"),
				("PAGE_TITLE",          pageTitle),
				("DISPLAY_NAME",        displayName),
				("STATUS_BANNER",       statusBanner),
				("ZPL_CONTENT_ENCODED", zplContentEncoded),
				("JS_VARS",             jsVars));

		public static string BuildDirPage(string listItems)
			=> $"<html><body><h2>E: DIRECTORY</h2><ul>{listItems}</ul></body></html>";
	}
}
