using LINQPad.Extensibility.DataContext;
using System.Windows;
using System.Windows.Controls;

namespace SwaggerContextDriver
{
    /// <summary>
    /// Interaction logic for ConnectionDialog.xaml
    /// </summary>
    public partial class ConnectionDialog : Window
	{
        ConnectionProperties _properties;

        public ConnectionDialog (IConnectionInfo cxInfo)
		{
			DataContext = _properties = new ConnectionProperties (cxInfo);
			Background = SystemColors.ControlBrush;
			InitializeComponent ();
		}	

		void btnOK_Click (object sender, RoutedEventArgs e)
		{
		    var password = (string)((PasswordBox)((Button)sender).Tag).Password;
		    _properties.Password = password;

            DialogResult = true;
		}
	}
}
