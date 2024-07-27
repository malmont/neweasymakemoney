namespace Easymakemoney.Controls;

public partial class FlyoutHeaderControl : StackLayout
{
	public FlyoutHeaderControl()
	{
		InitializeComponent();
		if(App.UserDetails != null)
		{
            lblUserRole.Text = App.UserDetails.Role;
            lblUserEmail.Text = App.UserDetails.Email;
		}
	}
}
