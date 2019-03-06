using MenuBuddy;
using Microsoft.Xna.Framework.Content;

namespace PlayFabBuddyLib.LoginScreen
{
	public class LoggingInMessageBox : MessageBoxScreen
	{
		public LoggingInMessageBox(ContentManager content = null) : base("Logging in...", string.Empty, content)
		{
			CoveredByOtherScreens = false;
			CoverOtherScreens = true;
		}

		protected override void AddButtons(StackLayout stack)
		{
		}
	}
}
