using MenuBuddy;
using Microsoft.Xna.Framework.Content;
using System.Threading.Tasks;

namespace PlayFabBuddyLib.LoginScreen
{
	public class LoggingInMessageBox : MessageBoxScreen
	{
		public LoggingInMessageBox(ContentManager content = null) : base("Logging in...", string.Empty, content)
		{
			CoveredByOtherScreens = false;
			CoverOtherScreens = true;
		}

		protected override Task AddButtons(StackLayout stack)
		{
			return Task.CompletedTask;
		}
	}
}
