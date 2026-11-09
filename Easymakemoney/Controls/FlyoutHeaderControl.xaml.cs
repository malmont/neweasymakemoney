using Easymakemoney.Models.Session;

namespace Easymakemoney.Controls;

public partial class FlyoutHeaderControl : StackLayout
{
	public FlyoutHeaderControl()
	{
		InitializeComponent();
	}

	public FlyoutHeaderControl(UserInfo? user) : this()
	{
		if (user != null)
		{
			lblUserRole.Text = "Administrateur";
			lblUserEmail.Text = user.Email;
		}
	}
}
