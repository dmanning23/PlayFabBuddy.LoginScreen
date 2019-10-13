﻿
namespace PlayFabBuddyLib.LoginScreen
{
	public abstract class LoginStyleSheet
	{
		#region Options

		public static bool AddFacebookButtonText { get; set; }

		public static string DisplayNameFontResource { get; set; }

		public static string FacebookButtonBackgroundImage { get; set; }

		public static string ButtonBackgroundImage { get; set; }

		#endregion //Options

		#region Methods

		static LoginStyleSheet()
		{
			DisplayNameFontResource = @"Fonts\ariblk";
			FacebookButtonBackgroundImage = "FacebookButton";
			ButtonBackgroundImage = string.Empty;
			AddFacebookButtonText = true;
		}

		#endregion //Methods
	}
}