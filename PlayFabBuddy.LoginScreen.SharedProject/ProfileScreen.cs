using FacebookLoginLib;
using FontBuddyLib;
using InputHelper;
using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PlayFab;
using PlayFab.ClientModels;
using PlayFabBuddyLib.Auth;
using PlayFabBuddyLib.DisplayName;
using ResolutionBuddy;
using System;
using System.Threading.Tasks;

namespace PlayFabBuddyLib.LoginScreen
{
	public class ProfileScreen : MessageBoxScreen
	{
		#region Propeties

		IPlayFabClient _playfab;
		IPlayFabAuthService Auth { get; set; }
		IFacebookService _facebook;
		IPlayFabDisplayNameService _displayNameService;

		/// <summary>
		/// Facebook errors are reported via a flag because Android doesn't let MenuBuddy pop up message boxes from another thread.
		/// </summary>
		bool _hasFacebookError { get; set; }
		Exception _facebookError { get; set; }

		TextEditWithDialog _displayName;
		IButton _logoutButton;
		IButton _facebookButton;

		#endregion //Propeties

		#region Methods

		public ProfileScreen(ContentManager content = null) : base("", "", content)
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
			_displayNameService = ScreenManager.Game.Services.GetService<IPlayFabDisplayNameService>();
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

			ControlStack.AddItem(new Label("Player Profile", Content, FontSize.Medium)
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

			AddDisplayNameMessage(controlSize);
			await AddDisplayName(controlSize);

			AddShim(64f);

			//Add the facebook button
			if (null != _facebook)
			{
				AddFacebookButton(controlSize);
				AddShim(64f);
			}

			AddLogoutButton(controlSize);

			AddShim(64f);
		}

		public override void UnloadContent()
		{
			base.UnloadContent();
			Auth.OnLoginSuccess -= Auth_OnLoginSuccess;
			Auth.OnPlayFabError -= Auth_OnPlayFabError;
			_facebook.OnLoginError -= Facebook_OnLoginError;
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

		private void AddDisplayNameMessage(Vector2 controlSize)
		{
			var label = new RelativeLayout()
			{
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
				Size = new Vector2(controlSize.X, controlSize.Y * 1.4f),
				Highlightable = false,
			};
			label.AddItem(new Label(@"You are logged in as:", Content, FontSize.Small)
			{
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
				Highlightable = false,
			});
			ControlStack.AddItem(label);

			AddShim();
		}

		private async Task AddDisplayName(Vector2 controlSize)
		{
			var font = new FontBuddyPlus();
			font.LoadContent(Content, LoginStyleSheet.DisplayNameFontResource, true, LoginStyleSheet.DisplayNameFontSize);

			var displayName = await _displayNameService.GetDisplayName();

			_displayName = new TextEditWithDialog(displayName, font)
			{
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
				Size = controlSize,
				HasOutline = true,
				MessageBoxTitle = "Email",
				MessageBoxDescription = "Enter your email address:",
				IsPassword = false,
				TextColor = StyleSheet.MessageBoxTextColor,
			};
			ControlStack.AddItem(_displayName);
			_displayName.OnPopupDialog += DisplayName_OnPopUp;

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
				HasBackground = false,
				Highlightable = false,
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
				_facebookButton.AddItem(new Label(@"f Link", Content, FontSize.Small)
				{
					Vertical = VerticalAlignment.Center,
					Horizontal = HorizontalAlignment.Center,
					Layer = 11,
					Highlightable = false,
				});
			}
			ControlStack.AddItem(_facebookButton);
			_facebookButton.OnClick += _facebookButton_OnClick; ;
		}

		private void AddLogoutButton(Vector2 controlSize)
		{
			_logoutButton = new RelativeLayoutButton()
			{
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
				Size = new Vector2(controlSize.X, controlSize.Y * 1.4f),
				HasBackground = true,
				Highlightable = false,
			};
			_logoutButton.AddItem(new Label(@"Logout", Content, FontSize.Small)
			{
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
				Highlightable = false,
			});
			ControlStack.AddItem(_logoutButton);
			_logoutButton.OnClick += Logout_OnClick;
		}

		#endregion //Add Controls

		#region Input Response

		private async void DisplayName_OnPopUp(object sender, ClickEventArgs e)
		{
			var displayName = await _displayNameService.GetDisplayName();
			var msgBox = new DisplayNameMessageBox("Please choose a display name:", displayName);
			msgBox.OnSelect += DisplayName_OnOk;
			await ScreenManager.AddScreen(msgBox);
		}

		private async void DisplayName_OnOk(object sender, ClickEventArgs e)
		{
			_displayName.Text = await _displayNameService.GetDisplayName();
		}

		private void _facebookButton_OnClick(object sender, ClickEventArgs e)
		{
			DisableButtons();
			Auth.Logout();
		}

		private void Logout_OnClick(object sender, ClickEventArgs e)
		{
			DisableButtons();
			Auth.Logout();
		}

		private void DisableButtons()
		{
			_displayName.Clickable = false;
			_logoutButton.Clickable = false;
			if (null != _facebookButton)
			{
				_facebookButton.Clickable = false;
			}
		}

		private void EnableButtons()
		{
			_displayName.Clickable = true;
			_logoutButton.Clickable = true;
			if (null != _facebookButton)
			{
				_facebookButton.Clickable = true;
			}
		}

		#endregion //Input Response

		#endregion //Methods
	}
}
