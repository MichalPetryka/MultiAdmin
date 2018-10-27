using MultiAdmin.MultiAdmin.Features;

namespace MultiAdmin.MultiAdmin.Commands
{
	[Feature]
	internal class Autoscale : Feature, IEventServerFull
	{
		private string config;

		public Autoscale(Server server) : base(server)
		{
		}

		public void OnServerFull()
		{
			if (!config.Equals("disabled"))
				if (!Server.IsConfigRunning(config))
					Server.NewInstance(config);
		}

		public override void Init()
		{
		}

		public override void OnConfigReload()
		{
			config = Server.ServerConfig.config.GetString("start_config_on_full", "disabled");
		}

		public override string GetFeatureDescription()
		{
			return "Auto-starts a new server once this one becomes full. (Requires servermod to function fully)";
		}

		public override string GetFeatureName()
		{
			return "Autoscale";
		}
	}
}