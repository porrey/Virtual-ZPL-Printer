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
using System.Net;
using VirtualPrinter.Repository.HostAddresses;
using Xunit;

namespace VirtualPrinter.Tests
{
	public class HostAddressRepositoryTests
	{
		[Fact]
		public async Task GetAllAsync_ReturnsAtLeastTwoAddresses()
		{
			HostAddressRepository repository = new();
			IEnumerable<IHostAddress> result = await repository.GetAllAsync();
			Assert.True(result.Count() >= 2);
		}

		[Fact]
		public async Task GetAllAsync_ContainsAnyAddress()
		{
			HostAddressRepository repository = new();
			IEnumerable<IHostAddress> result = await repository.GetAllAsync();
			Assert.Contains(result, a => a.IpAddress.Equals(IPAddress.Any));
		}

		[Fact]
		public async Task GetAllAsync_ContainsLoopbackAddress()
		{
			HostAddressRepository repository = new();
			IEnumerable<IHostAddress> result = await repository.GetAllAsync();
			Assert.Contains(result, a => a.IpAddress.Equals(IPAddress.Loopback));
		}

		[Fact]
		public async Task GetAsync_WithPredicate_ReturnsFilteredResults()
		{
			HostAddressRepository repository = new();
			IEnumerable<IHostAddress> result = await repository.GetAsync(a => a.IpAddress.Equals(IPAddress.Loopback));
			Assert.Single(result);
			Assert.Equal(IPAddress.Loopback, result.First().IpAddress);
		}
	}
}
