namespace MultiAdmin.Features
{
	internal class Restart : Feature, ICommand
	{
		public Restart(Server server) : base(server)
		{
		}

		public string GetCommand()
		{
			return "RESTART";
		}

		public string GetCommandDescription()
		{
			return "Restarts the game server (MultiAdmin will not restart, just the game)";
		}

		public string GetUsage()
		{
			return string.Empty;
		}

		public void OnCall(string[] args)
		{
			Server.SoftRestartServer();
		}

		public bool PassToGame()
		{
			return false;
		}

		public override string GetFeatureDescription()
		{
			return "Allows the game to be restarted without restarting MultiAdmin";
		}

		public override string GetFeatureName()
		{
			return "Restart Command";
		}

		public override void Init()
		{
		}

		public override void OnConfigReload()
		{
		}
	}
}
