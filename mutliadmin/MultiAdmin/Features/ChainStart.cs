using MultiAdmin.MultiAdmin.Features;

namespace MultiAdmin.MultiAdmin.Commands
{
	[Feature]
	internal class ChainStart : Feature, IEventServerStart
	{
		private bool dontstart;

		public ChainStart(Server server) : base(server)
		{
			dontstart = false;
		}


		public void OnServerStart()
		{
			if (!(string.IsNullOrWhiteSpace(Server.ConfigChain) || Server.ConfigChain.Trim().Equals("\"\"")) && !dontstart)
			{
				dontstart = true;
				Server.Write("Starting next with chained config:" + Server.ConfigChain);
				Server.NewInstance(Server.ConfigChain);
			}
		}

		public override void Init()
		{
		}

		public override string GetFeatureDescription()
		{
			return "Automatically starts the next server after the first one is done loading.";
		}

		public override string GetFeatureName()
		{
			return "ChainStart";
		}

		public override void OnConfigReload()
		{
		}
	}
}