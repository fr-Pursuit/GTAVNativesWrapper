using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GTAVNativesWrapper.Ui
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			Wrapper wrapper = new Wrapper(this);
			this.InitializeComponent();
			wrapper.InitUI();
			this.DataContext = wrapper.UiManager;
		}
	}
}
