using FacebookLoginLib;
using InputHelper;
using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using PlayFab;
using PlayFab.ClientModels;
using PlayFabBuddyLib.Auth;
using ResolutionBuddy;
using System;
using System.Threading.Tasks;

namespace PlayFabBuddyLib.LoginScreen
{
	public class LoginMessageBox : MessageBoxScreen
	{
		#region Propeties

		IPlayFabClient _playfab;
		IPlayFabAuthService Auth { get; set; }
		IFacebookService _facebook; 

		ICheckbox _rememberMe;
		TextEditWithDialog _email;
		TextEditWithDialog _password;
		IButton _loginButton;
		IButton _guestButton;
		IButton _facebookButton;

		#endregion //Propeties

		#region Methods

		public LoginMessageBox(ContentManager content = null) : base("", "", content)
		{
			CoveredByOtherScreens = false;
			CoverOtherScreens = true;
		}

		protected override void AddAddtionalControls()
		{
			base.AddAddtionalControls();

			_playfab = ScreenManager.Game.Services.GetService<IPlayFabClient>();
			Auth = ScreenManager.Game.Services.GetService<IPlayFabAuthService>();
			Auth.OnLoginSuccess -= Auth_OnLoginSuccess;
			Auth.OnPlayFabError -= Auth_OnPlayFabError;
			Auth.OnLoginSuccess += Auth_OnLoginSuccess;
			Auth.OnPlayFabError += Auth_OnPlayFabError;
			_facebook = ScreenManager.Game.Services.GetService<IFacebookService>();

			if (null == Auth)
			{
				throw new Exception("You forgot to add an IPlayFabAuthService to Game.Services");
			}

			ControlStack.AddItem(new Label("Sign In", Content, FontSize.Medium)
			{
				Horizontal = HorizontalAlignment.Center,
				Vertical = VerticalAlignment.Bottom,
				Highlightable = false,
			});
			AddShim(64f);

			var controlSize = new Vector2(Resolution.ScreenArea.Width * 0.8f, 48f);

			//create the email edit box
			AddEmailEditBox(controlSize);

			//create the password edit box
			AddPasswordEditBox(controlSize);

			//add the Remember checkbox
			AddRememberCheckbox(controlSize);

			//create the Login/Register button
			AddLogRegisterButton(controlSize);

			AddShim(64f);

			//Add the facebook button
			if (null != _facebook)
			{
				AddFacebookButton(controlSize);
				AddShim(16f);
			}
			
			//add the Guest button
			AddGuestButton(controlSize);
		}

		public override void UnloadContent()
		{
			base.UnloadContent();
			Auth.OnLoginSuccess -= Auth_OnLoginSuccess;
			Auth.OnPlayFabError -= Auth_OnPlayFabError;
		}

		private void AddShim(float height = 8f)
		{
			ControlStack.AddItem(new Shim() { Size = new Vector2(0, height) });
		}

		protected override void AddButtons(StackLayout stack)
		{
			//Don't add Ok or Cancel buttons to this message box, the user has to select one of the provided options.
		}

		private void Auth_OnPlayFabError(PlayFabError error)
		{
			switch (error.Error)
			{
				case PlayFabErrorCode.AccountNotFound:
					{
						ScreenManager.AddScreen(new AccountNotFoundMessageBox(Content), null);
					}
					break;
				default:
					{
						ScreenManager.AddScreen(new OkScreen(error.ErrorMessage, Content), null);
					}
					break;
			}

			EnableButtons();
		}

		private void Auth_OnLoginSuccess(LoginResult success)
		{
			ExitScreen();
		}

		#region Add Controls

		private void AddEmailEditBox(Vector2 controlSize)
		{
			var text = !String.IsNullOrEmpty(Auth.Email) ? Auth.Email : "Email address...";
			_email = new TextEditWithDialog(text, Content, FontSize.Small)
			{
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
				Size = controlSize,
				HasOutline = true,
				MessageBoxTitle = "Email",
				MessageBoxDescription = "Enter your email address:",
				IsPassword = false,
			};
			ControlStack.AddItem(_email);
			_email.OnPopupDialog += _email_OnPopupDialog;
			_email.OnTextEdited += Email_OnTextEdited;

			AddShim();
		}

		private void AddPasswordEditBox(Vector2 controlSize)
		{
			//do some special logic for the password text
			var hasPassword = !String.IsNullOrEmpty(Auth.Password);

			_password = new TextEditWithDialog(hasPassword ? Auth.Password : "Password...", Content, FontSize.Small)
			{
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
				Size = controlSize,
				HasOutline = true,
				MessageBoxTitle = "Password",
				MessageBoxDescription = "Enter your password:",
				IsPassword = hasPassword,
			};
			ControlStack.AddItem(_password);
			_password.OnPopupDialog += _password_OnPopupDialog;
			_password.OnTextEdited += Password_OnTextEdited;

			AddShim();
		}

		private void AddLogRegisterButton(Vector2 controlSize)
		{
			_loginButton = new RelativeLayoutButton()
			{
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
				Size = new Vector2(controlSize.X, controlSize.Y * 1.4f),
				HasBackground = true,
			};
			_loginButton.AddItem(new Label(@"Login/Register", Content, FontSize.Small)
			{
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
			});
			ControlStack.AddItem(_loginButton);
			_loginButton.OnClick += LoginRegister_OnClick;

			AddShim();
		}
		
		private void AddFacebookButton(Vector2 controlSize)
		{
			//add the facebook button
			_facebookButton = new RelativeLayoutButton()
			{
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
				Size = new Vector2(controlSize.X, controlSize.Y * 1.4f),
				HasBackground = true,
			};
			_facebookButton.AddItem(new Label(@"f Connect", Content, FontSize.Small)
			{
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
			});
			ControlStack.AddItem(_facebookButton);
			_facebookButton.OnClick += _facebookButton_OnClick; ;
		}

			private void AddGuestButton(Vector2 controlSize)
		{
			_guestButton = new RelativeLayoutButton()
			{
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
				Size = new Vector2(controlSize.X, controlSize.Y * 1.4f),
				HasBackground = true,
			};
			_guestButton.AddItem(new Label(@"Guest", Content, FontSize.Small)
			{
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
			});
			ControlStack.AddItem(_guestButton);
			_guestButton.OnClick += Guest_OnClick;
		}

		private void AddRememberCheckbox(Vector2 controlSize)
		{
			//add the relative layout for the whole row
			var remeberRow = new RelativeLayout()
			{
				Size = controlSize,
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
			};

			//add the button & the checkbox
			var rememberButton = new RelativeLayoutButton()
			{
				Size = new Vector2(controlSize.X / 2, controlSize.Y),
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Center,
			};
			_rememberMe = new Checkbox(Auth.RememberMe)
			{
				Size = new Vector2(24f, 24f),
				Horizontal = HorizontalAlignment.Left,
				Vertical = VerticalAlignment.Center,
				Highlightable = true,
			};
			rememberButton.AddItem(_rememberMe);
			rememberButton.AddItem(new Label(" Remember Me", Content, FontSize.Small)
			{
				Horizontal = HorizontalAlignment.Center,
				Vertical = VerticalAlignment.Center,
				Highlightable = false,
				Scale = 0.7f,
			});
			remeberRow.AddItem(rememberButton);
			_rememberMe.OnClick += RememberCheckbox_OnClick;
			rememberButton.OnClick += RemeberRow_OnClick;

			//add the button and the Forgot Password button
			var forgotButton = new RelativeLayoutButton()
			{
				Size = new Vector2(controlSize.X / 2, controlSize.Y),
				Horizontal = HorizontalAlignment.Right,
				Vertical = VerticalAlignment.Center,
			};
			forgotButton.AddItem(new Label("Forgot Password", Content, FontSize.Small)
			{
				Horizontal = HorizontalAlignment.Right,
				Vertical = VerticalAlignment.Center,
				Highlightable = true,
				Scale = 0.7f,
			});
			remeberRow.AddItem(forgotButton);
			forgotButton.OnClick += ForgotButton_OnClick;

			ControlStack.AddItem(remeberRow);
		}

		#endregion //Add Controls

		#region Input Response

		private void _email_OnPopupDialog(object sender, ClickEventArgs e)
		{
			if (string.IsNullOrEmpty(Auth.Email))
			{
				_email.Text = "";
			}
		}

		private void Email_OnTextEdited(object sender, TextChangedEventArgs e)
		{
			Auth.Email = e.Text;
		}

		private void _password_OnPopupDialog(object sender, ClickEventArgs e)
		{
			_password.IsPassword = true;
			if (string.IsNullOrEmpty(Auth.Password))
			{
				_password.Text = "";
			}
		}

		private void Password_OnTextEdited(object sender, TextChangedEventArgs e)
		{
			Auth.Password = e.Text;
		}

		private void LoginRegister_OnClick(object sender, ClickEventArgs e)
		{
			//check that an email and password were provided
			if (string.IsNullOrEmpty(Auth.Email))
			{
				ScreenManager.AddScreen(new OkScreen("Please enter your email address", Content), null);
			}
			else if (string.IsNullOrEmpty(Auth.Password))
			{
				ScreenManager.AddScreen(new OkScreen("Please enter your password", Content), null);
			}
			else
			{
				DisableButtons();
				Auth.AuthType = AuthType.EmailAndPassword;
				Task.Run(async () =>
				{
					await Auth.Authenticate();
				});
			}
		}

		private void _facebookButton_OnClick(object sender, ClickEventArgs e)
		{
			DisableButtons();
			Auth.AuthType = AuthType.Facebook;
			Task.Run(async () =>
			{
				await Auth.Authenticate();
			});
		}

		private void Guest_OnClick(object sender, ClickEventArgs e)
		{
			DisableButtons();
			Auth.AuthType = AuthType.Silent;
			Task.Run(async () =>
			{
				await Auth.Authenticate();
			});
		}

		private void RemeberRow_OnClick(object sender, ClickEventArgs e)
		{
			_rememberMe.IsChecked = !_rememberMe.IsChecked;
			RememberCheckbox_OnClick(sender, e);
		}

		private void RememberCheckbox_OnClick(object sender, ClickEventArgs e)
		{
			Auth.RememberMe = _rememberMe.IsChecked;
		}

		private void ForgotButton_OnClick(object sender, ClickEventArgs e)
		{
			if (string.IsNullOrEmpty(Auth.Email))
			{
				ScreenManager.AddScreen(new OkScreen("No email address provided.", Content));
			}
			else
			{
				Task.Run(async () =>
				{
					var result = await _playfab.SendAccountRecoveryEmailAsync(new SendAccountRecoveryEmailRequest()
					{
						Email = Auth.Email,
						TitleId = PlayFabSettings.TitleId
					});

					if (null != result.Error)
					{
						ScreenManager.AddScreen(new OkScreen(result.Error.ErrorMessage, Content));
					}
					else
					{
						ScreenManager.AddScreen(new OkScreen("An email was sent with instructions to reset the password.", Content));
					}
				});
			}
		}

		private void DisableButtons()
		{
			_loginButton.Clickable = false;
			_guestButton.Clickable = false;
			_facebookButton.Clickable = false;
		}

		private void EnableButtons()
		{
			_loginButton.Clickable = true;
			_guestButton.Clickable = true;
			_facebookButton.Clickable = true;
		}

		#endregion //Input Response

		#endregion //Methods
	}
}
