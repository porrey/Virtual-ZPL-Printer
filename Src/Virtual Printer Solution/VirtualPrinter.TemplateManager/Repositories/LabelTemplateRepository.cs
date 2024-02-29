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
using System.Linq.Expressions;
using System.Reflection;
using Diamond.Core.Repository;
using Microsoft.Extensions.Logging;
using VirtualPrinter.ApplicationSettings;

namespace VirtualPrinter.TemplateManager
{
	public class LabelTemplateRepository : DisposableObject, IReadOnlyRepository<ILabelTemplate>
	{
		public LabelTemplateRepository(ILogger<LabelTemplateRepository> logger, ISettings settings)
		{
			this.Name = this.GetType().Name.Replace("Repository", "");
			this.Logger = logger;
			this.Settings = settings;

			//
			// Map the template folder.
			//
			DirectoryInfo templateFolder = new($"{this.Settings.RootFolder.FullName}/Templates");

			//
			// Make sure the folder exists.
			//
			templateFolder.Create();

			//
			// Map the application folder.
			//
			DirectoryInfo applicationFolder = new($"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/Templates");

			//
			// Load the templates from the application folder.
			//
			IEnumerable<LabelTemplate> items1 = from tbl in applicationFolder.EnumerateFiles("*.zpl")
												where tbl.Exists
												select new LabelTemplate()
												{
													TemplateFile = tbl,
													Name = Path.GetFileNameWithoutExtension(tbl.Name),
													Zpl = File.ReadAllText(tbl.FullName)
												};

			//
			// Load the templates from the user folder.
			//
			IEnumerable<LabelTemplate> items2 = from tbl in templateFolder.EnumerateFiles("*.zpl")
												where tbl.Exists
												select new LabelTemplate()
												{
													TemplateFile = tbl,
													Name = Path.GetFileNameWithoutExtension(tbl.Name),
													Zpl = File.ReadAllText(tbl.FullName)
												};

			//
			// Combine the lists.
			//
			this.Items = items1.Union(items2).ToArray();
		}

		protected ILogger<LabelTemplateRepository> Logger { get; set; }
		protected ISettings Settings { get; set; }
		protected IList<ILabelTemplate> Items { get; } = [];
		public string Name { get; set; }

		public Task<IEnumerable<ILabelTemplate>> GetAllAsync()
		{
			return Task.FromResult<IEnumerable<ILabelTemplate>>(this.Items);
		}

		public Task<IEnumerable<ILabelTemplate>> GetAsync(Expression<Func<ILabelTemplate, bool>> predicate)
		{
			return Task.FromResult<IEnumerable<ILabelTemplate>>(this.Items.Where(predicate.Compile()).ToArray());
		}
	}
}
