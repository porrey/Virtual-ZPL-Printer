using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace VirtualZplPrinter
{
	public static class TestClient
	{
		public static async Task<(bool, string)> SendStringAsync(IPAddress ipaddress, int port, string text)
		{
			(bool result, string errorMessage) = (false, null);

			try
			{
				using (TcpClient client = new())
				{
					//
					// Connect to the local host.
					//
					await client.ConnectAsync(ipaddress, port);

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

				errorMessage = null;
				result = true;
			}
			catch (Exception ex)
			{
				errorMessage = $"Error: {ex.Message}";
				result = false;
			}

			return (result, errorMessage);
		}
	}
}
