using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace PrintClient
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			this.InitializeComponent();

			this.Zpl.Text = File.ReadAllText("./Samples/zpl.txt");
		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			using (TcpClient client = new())
			{
				await client.ConnectAsync(this.Printer.Text, Convert.ToInt32(this.Port.Text));

				using (Stream stream = client.GetStream())
				{
					byte[] buffer = ASCIIEncoding.UTF8.GetBytes(this.Zpl.Text);
					await stream.WriteAsync(buffer.AsMemory(0, buffer.Length));
					client.Close();
				}
			}
		}
	}
}
