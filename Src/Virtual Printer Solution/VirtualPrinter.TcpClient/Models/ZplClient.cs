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
		public async Task<(bool, string)> SendStringAsync(IPAddress ipAddress, int port, string text, int segmentSize = 1024, int delay = 0)
		{
			(bool result, string errorMessage) = (false, null);

			try
			{
				//
				// Break the text into segments.
				//
				IEnumerable<string> segments = text.Select((c, i) => new { c, i })
												   .GroupBy(x => x.i / segmentSize)
												   .Select(g => String.Join("", g.Select(y => y.c)))
												   .ToArray();

				using (TcpClient client = new())
				{
					//
					// Connect to the local host.
					//
					await client.ConnectAsync(ipAddress, port);

					//
					// Create a stream to send the text.
					//
					using (NetworkStream stream = client.GetStream())
					{
						foreach (string segment in segments)
						{
							//
							// Convert the text to a byte array.
							//
							byte[] buffer = ASCIIEncoding.UTF8.GetBytes(segment);

							//
							// Send the text.
							//
							await stream.WriteAsync(buffer.AsMemory(0, buffer.Length));

							//
							// Delay.
							//
							await Task.Delay(delay);
						}

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
