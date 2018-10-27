﻿namespace MultiAdmin.MultiAdmin.Commands
{
	internal class EventTest : Feature, IEventCrash, IEventMatchStart, IEventPlayerConnect, IEventPlayerDisconnect, IEventRoundEnd, IEventRoundStart, IEventServerFull, IEventServerPreStart, IEventServerStart, IEventServerStop
	{
		public EventTest(Server server) : base(server)
		{
		}

		public void OnCrash()
		{
			Server.Write("EVENTTEST Crash");
		}

		public void OnMatchStart()
		{
			Server.Write("EVENTTEST Match Start");
		}

		public void OnPlayerConnect(string name)
		{
			Server.Write("EVENTTEST player connect " + name);
		}

		public void OnPlayerDisconnect(string name)
		{
			Server.Write("EVENTTEST player disconnect " + name);
		}

		public void OnRoundEnd()
		{
			Server.Write("EVENTTEST on round end");
		}

		public void OnRoundStart()
		{
			Server.Write("EVENTTEST on round start");
		}

		public void OnServerFull()
		{
			Server.Write("EVENTTEST Server full event");
		}

		public void OnServerPreStart()
		{
			Server.Write("EVENTTEST on prestart");
		}

		public void OnServerStart()
		{
			Server.Write("EVENTTEST on start");
		}

		public void OnServerStop()
		{
			Server.Write("EVENTTEST on stop");
		}

		public override void Init()
		{
		}

		public override void OnConfigReload()
		{
		}

		public override string GetFeatureDescription()
		{
			return "Tests the events";
		}

		public override string GetFeatureName()
		{
			return "Test";
		}
	}
}