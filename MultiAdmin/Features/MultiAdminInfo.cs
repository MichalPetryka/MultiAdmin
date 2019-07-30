using System;

namespace MultiAdmin.Features
{
	internal class MultiAdminInfo : Feature, IEventServerPreStart, ICommand
	{
		public MultiAdminInfo(Server server) : base(server)
		{
		}

		public void OnCall(string[] args)
		{
			PrintInfo();
		}

		public string GetCommand()
		{
			return "INFO";
		}

		public bool PassToGame()
		{
			return false;
		}

		public string GetCommandDescription()
		{
			return GetFeatureDescription();
		}

		public string GetUsage()
		{
			return string.Empty;
		}

		public void OnServerPreStart()
		{
			PrintInfo();
		}

		public override void Init()
		{
		}

		public override void OnConfigReload()
		{
		}

		public void PrintInfo()
		{
			Server.Write($"MultiAdmin v{Program.MaVersion} (https://github.com/Grover-c13/MultiAdmin/)", ConsoleColor.DarkMagenta);
			Server.Write("Released under MIT License Copyright © Grover 2019", ConsoleColor.DarkMagenta);
		}

		public override string GetFeatureDescription()
		{
			return "Prints MultiAdmin license and version information";
		}

		public override string GetFeatureName()
		{
			return "MultiAdminInfo";
		}
	}
}
