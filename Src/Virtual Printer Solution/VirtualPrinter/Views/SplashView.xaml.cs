using System.Windows;
using VirtualZplPrinter.ViewModels;

namespace VirtualZplPrinter.Views
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
