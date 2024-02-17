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
using System.Net;
using System.Net.Sockets;
using Diamond.Core.Repository;

namespace VirtualPrinter.Repository.HostAddresses
{
	public class HostAddressRepository : DisposableObject, IReadOnlyRepository<IHostAddress>
	{
		public HostAddressRepository()
		{
			this.Name = this.GetType().Name.Replace("Repository", "");

			this.Items.Add(new HostAddress() { Address = IPAddress.Any.ToString(), IpAddress = IPAddress.Any });
			this.Items.Add(new HostAddress() { Address = IPAddress.Loopback.ToString(), IpAddress = IPAddress.Loopback });

			IPHostEntry entry = Dns.GetHostEntry(Dns.GetHostName());
			foreach (IPAddress a in entry.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork))
			{
				this.Items.Add(new HostAddress() { Address = a.ToString(), IpAddress = a });
			}
		}

		protected IList<IHostAddress> Items { get; } = [];
		public string Name { get; set; }

		public Task<IEnumerable<IHostAddress>> GetAllAsync()
		{
			return Task.FromResult<IEnumerable<IHostAddress>>(this.Items);
		}

		public Task<IEnumerable<IHostAddress>> GetAsync(Expression<Func<IHostAddress, bool>> predicate)
		{
			return Task.FromResult<IEnumerable<IHostAddress>>(this.Items.Where(predicate.Compile()).ToArray());
		}
	}
}
