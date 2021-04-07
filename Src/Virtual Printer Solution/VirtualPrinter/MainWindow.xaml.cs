using System.Reflection;
using System.Windows;
using VirtualPrinter.ViewModels;

namespace VirtualPrinter
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			this.InitializeComponent();
			this.DataContext = new MainViewModel();
		}
	}
}
