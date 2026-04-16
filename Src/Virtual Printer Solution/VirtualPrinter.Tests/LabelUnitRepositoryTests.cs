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
using UnitsNet.Units;
using VirtualPrinter.Repository.LabelParameters;
using Xunit;

namespace VirtualPrinter.Tests
{
	public class LabelUnitRepositoryTests
	{
		[Fact]
		public async Task GetAllAsync_ReturnsThreeUnits()
		{
			LabelUnitRepository repository = new();
			IEnumerable<ILabelUnit> result = await repository.GetAllAsync();
			Assert.Equal(3, result.Count());
		}

		[Fact]
		public async Task GetAllAsync_ContainsInchMillimeterCentimeter()
		{
			LabelUnitRepository repository = new();
			IEnumerable<ILabelUnit> result = await repository.GetAllAsync();
			LengthUnit[] units = result.Select(u => u.Unit).ToArray();
			Assert.Contains(LengthUnit.Inch, units);
			Assert.Contains(LengthUnit.Millimeter, units);
			Assert.Contains(LengthUnit.Centimeter, units);
		}

		[Fact]
		public async Task GetAsync_WithPredicate_ReturnsFilteredResults()
		{
			LabelUnitRepository repository = new();
			IEnumerable<ILabelUnit> result = await repository.GetAsync(u => u.Unit == LengthUnit.Inch);
			Assert.Single(result);
		}
	}
}
