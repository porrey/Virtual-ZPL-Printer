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
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Diamond.Core.Repository;
using VirtualPrinter.Models;

namespace VirtualPrinter.Repositories
{
	public class HostRepository : DisposableObject, IReadOnlyRepository<Host>
	{
		public HostRepository()
		{
			this.Name = this.GetType().Name.Replace("Repository", "");

			this.Items.Add(new Host() { Address = IPAddress.Any.ToString() });
			this.Items.Add(new Host() { Address = IPAddress.Loopback.ToString() });

			IPHostEntry entry = Dns.GetHostEntry(Dns.GetHostName());
			IPAddress a = entry.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault();

			this.Items.Add(new Host() { Address = a.ToString() });
		}

		protected IList<Host> Items { get; } = new List<Host>();
		public string Name { get; set; }

		public Task<IEnumerable<Host>> GetAllAsync()
		{
			return Task.FromResult<IEnumerable<Host>>(this.Items);
		}

		public Task<IEnumerable<Host>> GetAsync(Expression<Func<Host, bool>> predicate)
		{
			return Task.FromResult<IEnumerable<Host>>(this.Items.Where(predicate.Compile()).ToArray());
		}
	}
}
