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
using VirtualPrinter.Repository.LabelParameters;
using Xunit;

namespace VirtualPrinter.Tests
{
	public class LabelRotationRepositoryTests
	{
		[Fact]
		public async Task GetAllAsync_ReturnsFourRotations()
		{
			LabelRotationRepository repository = new();
			IEnumerable<ILabelRotation> result = await repository.GetAllAsync();
			Assert.Equal(4, result.Count());
		}

		[Fact]
		public async Task GetAllAsync_ContainsExpectedRotationValues()
		{
			LabelRotationRepository repository = new();
			IEnumerable<ILabelRotation> result = await repository.GetAllAsync();
			int[] values = result.Select(r => r.Value).ToArray();
			Assert.Contains(0, values);
			Assert.Contains(90, values);
			Assert.Contains(180, values);
			Assert.Contains(270, values);
		}

		[Fact]
		public async Task GetAsync_WithPredicate_ReturnsFilteredResults()
		{
			LabelRotationRepository repository = new();
			IEnumerable<ILabelRotation> result = await repository.GetAsync(r => r.Value == 90);
			Assert.Single(result);
			Assert.Equal(90, result.First().Value);
		}

		[Fact]
		public async Task GetAsync_WithNoMatch_ReturnsEmpty()
		{
			LabelRotationRepository repository = new();
			IEnumerable<ILabelRotation> result = await repository.GetAsync(r => r.Value == 45);
			Assert.Empty(result);
		}
	}
}
