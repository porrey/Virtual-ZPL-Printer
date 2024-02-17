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
using Diamond.Core.Repository;

namespace VirtualPrinter.Repository.LabelParameters
{
	public class LabelUnitRepository : DisposableObject, IReadOnlyRepository<ILabelUnit>
	{
		public LabelUnitRepository()
		{
			this.Name = this.GetType().Name.Replace("Repository", "");
			this.Items.Add(new LabelUnit() { Unit = UnitsNet.Units.LengthUnit.Inch });
			this.Items.Add(new LabelUnit() { Unit = UnitsNet.Units.LengthUnit.Millimeter });
			this.Items.Add(new LabelUnit() { Unit = UnitsNet.Units.LengthUnit.Centimeter });
		}

		protected IList<ILabelUnit> Items { get; } = [];
		public string Name { get; set; }

		public Task<IEnumerable<ILabelUnit>> GetAllAsync()
		{
			return Task.FromResult<IEnumerable<ILabelUnit>>(this.Items);
		}

		public Task<IEnumerable<ILabelUnit>> GetAsync(Expression<Func<ILabelUnit, bool>> predicate)
		{
			return Task.FromResult<IEnumerable<ILabelUnit>>(this.Items.Where(predicate.Compile()).ToArray());
		}
	}
}
