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
using System.Net.Sockets;
using System.Text;

namespace VirtualPrinter.TestClient
{
	internal class ZplClient : IZplClient
	{
		public async Task<(bool, string)> SendStringAsync(IPAddress ipAddress, int port, string text)
		{
			(bool result, string errorMessage) = (false, null);

			try
			{
				using (TcpClient client = new())
				{
					//
					// Connect to the local host.
					//
					await client.ConnectAsync(ipAddress, port);

					//
					// Create a stream to send the text.
					//
					using (Stream stream = client.GetStream())
					{
						//
						// Convert the text to a byte array.
						//
						byte[] buffer = ASCIIEncoding.UTF8.GetBytes(text);

						//
						// Send the text.
						//
						await stream.WriteAsync(buffer.AsMemory(0, buffer.Length));

						//
						// Close the connection.
						//
						client.Close();
					}
				}

				result = true;
			}
			catch (Exception ex)
			{
				errorMessage = $"Error: {ex.Message}";
			}

			return (result, errorMessage);
		}
	}
}
