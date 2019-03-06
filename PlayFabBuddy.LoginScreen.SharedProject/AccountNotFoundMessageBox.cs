using InputHelper;
using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using PlayFabBuddyLib.Auth;
using ResolutionBuddy;
using System;
using System.Threading.Tasks;

namespace PlayFabBuddyLib.LoginScreen
{
	public class AccountNotFoundMessageBox : MessageBoxScreen
	{
		#region Propeties

		IPlayFabAuthService Auth { get; set; }

		TextEditWithDialog _password;

		const string message = "User account not found. If you would like to register, retype your password and click \"Create Account\"";

		#endregion //Propeties

		#region Methods

		public AccountNotFoundMessageBox(ContentManager content = null) : base(message, "", content)
		{
			CoveredByOtherScreens = false;
			CoverOtherScreens = true;
		}

		protected override void AddAddtionalControls()
		{
			base.AddAddtionalControls();

			OkText = "Create Account";
			CancelText = "Cancel";

			Auth = ScreenManager.Game.Services.GetService<IPlayFabAuthService>();

			if (null == Auth)
			{
				throw new Exception("You forgot to add an IPlayFabAuthService to Game.Services");
			}

			//add a shim between the text and the buttons
			ControlStack.AddItem(new Shim() { Size = new Vector2(0, 16f) });

			var controlSize = new Vector2(Resolution.ScreenArea.Width * 0.8f, 48f);

			//create the password edit box
			AddPasswordEditBox(controlSize);

			OnSelect += AccountNotFoundMessageBox_OnSelect;
		}

		private void AddShim()
		{
			ControlStack.AddItem(new Shim() { Size = new Vector2(0, 8f) });
		}

		#region Add Controls

		private void AddPasswordEditBox(Vector2 controlSize)
		{
			_password = new TextEditWithDialog("Password...", Content, FontSize.Small)
			{
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
				Size = controlSize,
				HasOutline = true,
				MessageBoxTitle = "Password",
				MessageBoxDescription = "Retype your password:",
				IsPassword = false,
			};
			ControlStack.AddItem(_password);
			_password.OnPopupDialog += _password_OnPopupDialog;
			_password.OnTextEdited += Password_OnTextEdited;

			AddShim();
		}

		#endregion //Add Controls

		#region Input Response

		private void _password_OnPopupDialog(object sender, InputHelper.ClickEventArgs e)
		{
			_password.IsPassword = true;
			_password.Text = string.Empty;
		}

		private void Password_OnTextEdited(object sender, TextChangedEventArgs e)
		{
			_password.Text = e.Text;
		}

		private void AccountNotFoundMessageBox_OnSelect(object sender, ClickEventArgs e)
		{
			if (_password.Text != Auth.Password)
			{
				ScreenManager.AddScreen(new OkScreen("The passwords didn't match."));
			}
			else
			{
				Auth.AuthType = AuthType.RegisterPlayFabAccount;
				Task.Run(async () =>
				{
					await Auth.Authenticate();
				});
			}
		}

		#endregion //Input Response

		#endregion //Methods
	}
}
