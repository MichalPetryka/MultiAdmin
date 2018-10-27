using MultiAdmin.MultiAdmin.Features;

namespace MultiAdmin.MultiAdmin.Commands
{
	[Feature]
	internal class InactivityShutdown : Feature, IEventRoundStart, IEventRoundEnd, IEventTick
	{
		private long roundEndTime;
		private int waitFor;
		private bool waiting;

		public InactivityShutdown(Server server) : base(server)
		{
		}

		public void OnRoundEnd()
		{
			roundEndTime = Utils.GetUnixTime();
			waiting = true;
		}

		public void OnRoundStart()
		{
			waiting = false;
		}

		public void OnTick()
		{
			if (waitFor > 0 && waiting)
			{
				long elapsed = Utils.GetUnixTime() - roundEndTime;

				if (elapsed >= waitFor)
				{
					Server.Write("Server has been inactive for " + waitFor + " seconds, shutting down");
					Server.StopServer();
				}
			}
		}

		public override void Init()
		{
			roundEndTime = Utils.GetUnixTime();
		}

		public override void OnConfigReload()
		{
			waitFor = Server.ServerConfig.config.GetInt("shutdown_once_empty_for", -1);
		}


		public override string GetFeatureDescription()
		{
			return "Stops the server after a period inactivity.";
		}

		public override string GetFeatureName()
		{
			return "Stop Server once Inactive";
		}
	}
}