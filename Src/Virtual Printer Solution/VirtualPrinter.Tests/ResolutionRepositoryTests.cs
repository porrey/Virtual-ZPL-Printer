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
	public class ResolutionRepositoryTests
	{
		[Fact]
		public async Task GetAllAsync_ReturnsFourResolutions()
		{
			ResolutionRepository repository = new();
			IEnumerable<IResolution> result = await repository.GetAllAsync();
			Assert.Equal(4, result.Count());
		}

		[Fact]
		public async Task GetAllAsync_ContainsExpectedDpmmValues()
		{
			ResolutionRepository repository = new();
			IEnumerable<IResolution> result = await repository.GetAllAsync();
			int[] values = result.Select(r => r.Dpmm).ToArray();
			Assert.Contains(6, values);
			Assert.Contains(8, values);
			Assert.Contains(12, values);
			Assert.Contains(24, values);
		}

		[Fact]
		public async Task GetAsync_WithPredicate_ReturnsFilteredResults()
		{
			ResolutionRepository repository = new();
			IEnumerable<IResolution> result = await repository.GetAsync(r => r.Dpmm == 8);
			Assert.Single(result);
		}

		[Fact]
		public async Task Dpi_IsCalculatedCorrectly()
		{
			ResolutionRepository repository = new();
			IEnumerable<IResolution> result = await repository.GetAllAsync();
			IResolution res8 = result.First(r => r.Dpmm == 8);
			Assert.Equal(8 * 25.4, res8.Dpi, 5);
		}
	}
}
