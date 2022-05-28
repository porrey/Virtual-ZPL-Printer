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
using System.Linq.Expressions;
using System.Threading.Tasks;
using Diamond.Core.Repository;
using VirtualZplPrinter.Models;

namespace VirtualZplPrinter.Repositories
{
	public class ResolutionRepository : DisposableObject, IReadOnlyRepository<Resolution>
	{
		public ResolutionRepository()
		{
			this.Name = this.GetType().Name.Replace("Repository", "");
			this.Items.Add(new Resolution() { Dpmm = 6 });
			this.Items.Add(new Resolution() { Dpmm = 8 });
			this.Items.Add(new Resolution() { Dpmm = 12 });
			this.Items.Add(new Resolution() { Dpmm = 24 });
		}

		protected IList<Resolution> Items { get; } = new List<Resolution>();
		public string Name { get; set; }

		public Task<IEnumerable<Resolution>> GetAllAsync()
		{
			return Task.FromResult<IEnumerable<Resolution>>(this.Items);
		}

		public Task<IEnumerable<Resolution>> GetAsync(Expression<Func<Resolution, bool>> predicate)
		{
			return Task.FromResult<IEnumerable<Resolution>>(this.Items.Where(predicate.Compile()).ToArray());
		}
	}
}
