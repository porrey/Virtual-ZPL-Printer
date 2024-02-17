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
	public class LabelRotationRepository : DisposableObject, IReadOnlyRepository<ILabelRotation>
	{
		public LabelRotationRepository()
		{
			this.Name = this.GetType().Name.Replace("Repository", "");
			this.Items.Add(new LabelRotation() { Label = "None", Value = 0 });
			this.Items.Add(new LabelRotation() { Label = "90˚ Clockwise", Value = 90 });
			this.Items.Add(new LabelRotation() { Label = "180˚ Clockwise", Value = 180 });
			this.Items.Add(new LabelRotation() { Label = "270˚ Clockwise", Value = 270 });
		}

		protected IList<ILabelRotation> Items { get; } = [];
		public string Name { get; set; }

		public Task<IEnumerable<ILabelRotation>> GetAllAsync()
		{
			return Task.FromResult<IEnumerable<ILabelRotation>>(this.Items);
		}

		public Task<IEnumerable<ILabelRotation>> GetAsync(Expression<Func<ILabelRotation, bool>> predicate)
		{
			return Task.FromResult<IEnumerable<ILabelRotation>>(this.Items.Where(predicate.Compile()).ToArray());
		}
	}
}
