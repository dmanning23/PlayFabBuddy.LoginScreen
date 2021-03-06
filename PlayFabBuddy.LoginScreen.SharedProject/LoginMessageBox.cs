﻿using FacebookLoginLib;
using FontBuddyLib;
using InputHelper;
using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
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

		/// <summary>
		/// Facebook errors are reported via a flag because Android doesn't let MenuBuddy pop up message boxes from another thread.
		/// </summary>
		bool _hasFacebookError { get; set; }
		Exception _facebookError { get; set; }

		#endregion //Propeties

		#region Methods

		public LoginMessageBox(ContentManager content = null) : base("", "", content)
		{
			CoveredByOtherScreens = false;
			CoverOtherScreens = true;
			_hasFacebookError = false;
		}

		public override async Task LoadContent()
		{
			await base.LoadContent();

			AddCancelButton();
		}

		protected override async Task AddAdditionalControls()
		{
			await base.AddAdditionalControls();

			_playfab = ScreenManager.Game.Services.GetService<IPlayFabClient>();
			Auth = ScreenManager.Game.Services.GetService<IPlayFabAuthService>();
			Auth.OnLoginSuccess -= Auth_OnLoginSuccess;
			Auth.OnPlayFabError -= Auth_OnPlayFabError;
			Auth.OnLoginSuccess += Auth_OnLoginSuccess;
			Auth.OnPlayFabError += Auth_OnPlayFabError;
			_facebook = ScreenManager.Game.Services.GetService<IFacebookService>();

			if (null != _facebook)
			{
				_facebook.OnLoginError -= Facebook_OnLoginError;
				_facebook.OnLoginError += Facebook_OnLoginError;
			}

			if (null == Auth)
			{
				throw new Exception("You forgot to add an IPlayFabAuthService to Game.Services");
			}

			AddShim(64f);

			ControlStack.AddItem(new Label("Sign In", Content, FontSize.Medium, StyleSheet.MediumFontResource, null, StyleSheet.MediumFontSize)
			{
				Horizontal = HorizontalAlignment.Center,
				Vertical = VerticalAlignment.Bottom,
				Highlightable = false,
				TextColor = StyleSheet.MessageBoxTextColor,
				ShadowColor = Color.Transparent,
				Scale = 1.3f,
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

			AddShim(64f);
		}

		public override void UnloadContent()
		{
			base.UnloadContent();
			Auth.OnLoginSuccess -= Auth_OnLoginSuccess;
			Auth.OnPlayFabError -= Auth_OnPlayFabError;

			if (null != _facebook)
			{
				_facebook.OnLoginError -= Facebook_OnLoginError;
			}
		}

		public override async void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

			if (_hasFacebookError)
			{
				_hasFacebookError = false;
				if (null != _facebookError)
				{
					await ScreenManager.AddScreen(new OkScreen(_facebookError.Message)).ConfigureAwait(false);
				}
				else
				{
					await ScreenManager.AddScreen(new OkScreen("An error occurred logging into FaceBook.")).ConfigureAwait(false);
				}
				_facebookError = null;
			}
		}

		private void AddShim(float height = 8f)
		{
			ControlStack.AddItem(new Shim() { Size = new Vector2(0, height) });
		}

		protected override Task AddButtons(StackLayout stack)
		{
			//Don't add Ok or Cancel buttons to this message box, the user has to select one of the provided options.
			return Task.CompletedTask;
		}

		private async void Auth_OnPlayFabError(PlayFabError error)
		{
			switch (error.Error)
			{
				case PlayFabErrorCode.AccountNotFound:
					{
						await ScreenManager.AddScreen(new AccountNotFoundMessageBox(Content), null).ConfigureAwait(false);
					}
					break;
				default:
					{
						await ScreenManager.AddScreen(new OkScreen(error.ErrorMessage, Content), null).ConfigureAwait(false);
					}
					break;
			}

			EnableButtons();
		}

		private void Auth_OnLoginSuccess(LoginResult success)
		{
			ExitScreen();
		}

		private void Facebook_OnLoginError(Exception ex)
		{
			_facebookError = ex;
			_hasFacebookError = true;
		}

		#region Add Controls

		private void AddEmailEditBox(Vector2 controlSize)
		{
			var font = new FontBuddyPlus();
			font.LoadContent(Content, LoginStyleSheet.DisplayNameFontResource, true, LoginStyleSheet.DisplayNameFontSize);

			var text = !String.IsNullOrEmpty(Auth.Email) ? Auth.Email : "Email address...";
			_email = new TextEditWithDialog(text, font)
			{
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
				Size = controlSize,
				HasOutline = true,
				MessageBoxTitle = "Email",
				MessageBoxDescription = "Enter your email address:",
				IsPassword = false,
				TextColor = StyleSheet.MessageBoxTextColor,
				Highlightable = true,
			};
			ControlStack.AddItem(_email);
			_email.OnPopupDialog += _email_OnPopupDialog;
			_email.OnTextEdited += Email_OnTextEdited;

			_email.TextLabel.Highlightable = true;

			AddShim();

			AddMenuItem(_email);
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
				TextColor = StyleSheet.MessageBoxTextColor,
				Highlightable = true,
			};
			ControlStack.AddItem(_password);
			_password.OnPopupDialog += _password_OnPopupDialog;
			_password.OnTextEdited += Password_OnTextEdited;

			AddShim();

			AddMenuItem(_password);
		}

		private void AddLogRegisterButton(Vector2 controlSize)
		{
			_loginButton = CreateButton(controlSize, @"Login/Register");
			ControlStack.AddItem(_loginButton);
			_loginButton.OnClick += LoginRegister_OnClick;

			AddShim();

			AddMenuItem(_loginButton);
		}

		private IButton CreateButton(Vector2 controlSize, string text)
		{
			//check if there is a button background image
			var hasBackgroundImage = !string.IsNullOrEmpty(LoginStyleSheet.ButtonBackgroundImage);
			var button = new RelativeLayoutButton()
			{
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
				Size = new Vector2(controlSize.X, controlSize.Y * 1.9f),
				HasBackground = !hasBackgroundImage,
				Highlightable = true,
			};

			if (hasBackgroundImage)
			{
				button.AddItem(new Image(Content.Load<Texture2D>(LoginStyleSheet.ButtonBackgroundImage))
				{
					Vertical = VerticalAlignment.Center,
					Horizontal = HorizontalAlignment.Center,
					Highlightable = false,
					FillRect = true,
					Size = button.Rect.Size.ToVector2(),
					PulsateOnHighlight = false,
					Layer = 10,
				});
			}

			button.AddItem(new Label(text, Content, FontSize.Small)
			{
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
				Highlightable = true,
				Layer = 11,
			});

			return button;
		}

		private void AddFacebookButton(Vector2 controlSize)
		{
			//add the facebook button
			_facebookButton = new RelativeLayoutButton()
			{
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
				Size = new Vector2(controlSize.X, controlSize.Y * 1.9f),
				HasBackground = false,
				Highlightable = true,
			};
			_facebookButton.AddItem(new Image(Content.Load<Texture2D>(LoginStyleSheet.FacebookButtonBackgroundImage))
			{
				Size = _facebookButton.Rect.Size.ToVector2(),
				FillRect = true,
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
				Layer = 10,
			});
			if (LoginStyleSheet.AddFacebookButtonText)
			{
				_facebookButton.AddItem(new Label(@"f Connect", Content, FontSize.Small)
				{
					Vertical = VerticalAlignment.Center,
					Horizontal = HorizontalAlignment.Center,
					Layer = 11,
					Highlightable = false,
				});
			}
			ControlStack.AddItem(_facebookButton);
			
			_facebookButton.OnClick += _facebookButton_OnClick;

			AddMenuItem(_loginButton);
		}

		private void AddGuestButton(Vector2 controlSize)
		{
			_guestButton = CreateButton(controlSize, @"Guest");
			ControlStack.AddItem(_guestButton);
			_guestButton.OnClick += Guest_OnClick;

			AddMenuItem(_guestButton);
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
				Highlightable = true,
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
				Highlightable = true,
				Scale = 0.7f,
				TextColor = StyleSheet.MessageBoxTextColor,
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
				Highlightable = true,
			};
			forgotButton.AddItem(new Label("Forgot Password", Content, FontSize.Small)
			{
				Horizontal = HorizontalAlignment.Right,
				Vertical = VerticalAlignment.Center,
				Highlightable = true,
				Scale = 0.7f,
				TextColor = StyleSheet.MessageBoxTextColor,
			});

			remeberRow.AddItem(forgotButton);
			forgotButton.OnClick += ForgotButton_OnClick;

			ControlStack.AddItem(remeberRow);

			AddMenuItem(rememberButton, 10);
			AddMenuItem(forgotButton, 20);
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

		private async void LoginRegister_OnClick(object sender, ClickEventArgs e)
		{
			//check that an email and password were provided
			if (string.IsNullOrEmpty(Auth.Email))
			{
				await ScreenManager.AddScreen(new OkScreen("Please enter your email address", Content), null).ConfigureAwait(false);
			}
			else if (string.IsNullOrEmpty(Auth.Password))
			{
				await ScreenManager.AddScreen(new OkScreen("Please enter your password", Content), null).ConfigureAwait(false);
			}
			else
			{
				DisableButtons();
				Auth.AuthType = AuthType.EmailAndPassword;
				await Auth.Authenticate().ConfigureAwait(false);
			}
		}

		private async void _facebookButton_OnClick(object sender, ClickEventArgs e)
		{
			DisableButtons();
			Auth.AuthType = AuthType.Facebook;
			await Auth.Authenticate().ConfigureAwait(false);
		}

		private async void Guest_OnClick(object sender, ClickEventArgs e)
		{
			DisableButtons();
			Auth.AuthType = AuthType.Silent;
			await Auth.Authenticate().ConfigureAwait(false);
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

		private async void ForgotButton_OnClick(object sender, ClickEventArgs e)
		{
			if (string.IsNullOrEmpty(Auth.Email))
			{
				await ScreenManager.AddScreen(new OkScreen("No email address provided.", Content)).ConfigureAwait(false);
			}
			else
			{
				var result = await _playfab.SendAccountRecoveryEmailAsync(new SendAccountRecoveryEmailRequest()
				{
					Email = Auth.Email,
					TitleId = PlayFabSettings.staticSettings.TitleId
				}).ConfigureAwait(false);

				if (null != result.Error)
				{
					await ScreenManager.AddScreen(new OkScreen(result.Error.ErrorMessage, Content)).ConfigureAwait(false);
				}
				else
				{
					await ScreenManager.AddScreen(new OkScreen("An email was sent with instructions to reset the password.", Content)).ConfigureAwait(false);
				}
			}
		}

		private void DisableButtons()
		{
			_loginButton.Clickable = false;
			_guestButton.Clickable = false;
			if (null != _facebookButton)
			{
				_facebookButton.Clickable = false;
			}
		}

		private void EnableButtons()
		{
			_loginButton.Clickable = true;
			_guestButton.Clickable = true;
			if (null != _facebookButton)
			{
				_facebookButton.Clickable = true;
			}
		}

		#endregion //Input Response

		#endregion //Methods
	}
}
