using InputHelper;
using MenuBuddy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using PlayFabBuddyLib.Auth;
using ResolutionBuddy;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PlayFabBuddyLib.LoginScreen
{
	public class DisplayNameMessageBox : MessageBoxScreen
	{
		#region Propeties

		IPlayFabDisplayNameService _displayNameService;

		TextEditWithDialog DisplayNameEditBox;
		IButton _updateButton;

		string _displayName;

		#endregion //Propeties

		#region Methods

		public DisplayNameMessageBox(string message, string displayName, ContentManager content = null) : base(message, "", content)
		{
			_displayName = displayName;
			CoveredByOtherScreens = false;
			CoverOtherScreens = true;
		}

		protected override void AddAddtionalControls()
		{
			base.AddAddtionalControls();

			_displayNameService = ScreenManager.Game.Services.GetService<IPlayFabDisplayNameService>();

			if (null == _displayNameService)
			{
				throw new Exception("You forgot to add a DisplayNameService to Game.Services");
			}

			var controlSize = new Vector2(Resolution.ScreenArea.Width * 0.8f, 48f);

			//create the email edit box
			AddDisplayNameEditBox(controlSize);

			AddShim(32f);

			//add the Guest button
			AddUpdateButton(controlSize);
		}

		public override void UnloadContent()
		{
			base.UnloadContent();
		}

		private void AddShim(float height = 8f)
		{
			ControlStack.AddItem(new Shim() { Size = new Vector2(0, height) });
		}

		protected override void AddButtons(StackLayout stack)
		{
			//Don't add Ok or Cancel buttons to this message box, the user has to select one of the provided options.
		}

		#region Add Controls

		private void AddDisplayNameEditBox(Vector2 controlSize)
		{
			var text = !String.IsNullOrEmpty(_displayName) ? _displayName : "Display Name...";
			DisplayNameEditBox = new TextEditWithDialog(text, Content, FontSize.Small)
			{
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
				Size = controlSize,
				HasOutline = true,
				MessageBoxTitle = "Display Name",
				MessageBoxDescription = "Enter your display name:",
				IsPassword = false,
			};
			ControlStack.AddItem(DisplayNameEditBox);
			DisplayNameEditBox.OnPopupDialog += DisplayName_OnPopupDialog;
			DisplayNameEditBox.OnTextEdited += DisplayName_OnTextEdited;

			AddShim();
		}

		private void AddUpdateButton(Vector2 controlSize)
		{
			_updateButton = new RelativeLayoutButton()
			{
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
				Size = new Vector2(controlSize.X, controlSize.Y * 1.4f),
				HasBackground = true,
			};
			_updateButton.AddItem(new Label(@"Update Display Name", Content, FontSize.Small)
			{
				Vertical = VerticalAlignment.Center,
				Horizontal = HorizontalAlignment.Center,
			});
			ControlStack.AddItem(_updateButton);
			_updateButton.OnClick += Update_OnClick;
		}

		#endregion //Add Controls

		#region Input Response

		private void DisplayName_OnPopupDialog(object sender, ClickEventArgs e)
		{
			if (string.IsNullOrEmpty(_displayName))
			{
				DisplayNameEditBox.Text = "";
			}
		}

		private void DisplayName_OnTextEdited(object sender, TextChangedEventArgs e)
		{
			_displayName = e.Text;
		}

		private void Update_OnClick(object sender, ClickEventArgs e)
		{
			DisableButtons();
			Task.Run(async () =>
			{
				var result = await _displayNameService.SetDisplayName(_displayName);
				if (!string.IsNullOrEmpty(result))
				{
					ScreenManager.AddScreen(new OkScreen(result, Content));
					EnableButtons();
				}
				else
				{
					ExitScreen();
				}
			});
		}

		private void DisableButtons()
		{
			_updateButton.Clickable = false;
		}

		private void EnableButtons()
		{
			_updateButton.Clickable = true;
		}

		#endregion //Input Response

		#endregion //Methods
	}
}
