using System.Windows;
using VirtualPrinter.ViewModels;

namespace VirtualPrinter.Views
{
	public partial class SplashView : Window
    {
        public SplashView(SplashViewModel viewModel)
        {
            this.DataContext = viewModel;
            this.InitializeComponent();
        }
    }
}
