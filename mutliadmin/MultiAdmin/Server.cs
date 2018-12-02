using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using MultiAdmin.MultiAdmin.Commands;
using MultiAdmin.MultiAdmin.Features;

namespace MultiAdmin.MultiAdmin
{
	public class Server
	{
		public static readonly string MA_VERSION = "2.1.3";
		private readonly string maLogLocation;
		private readonly bool multiMode;
		private readonly Thread printerThread;

		private readonly Thread readerThread;
		private readonly List<IEventTick> tick; // we want a tick only list since its the only event that happens constantly, all the rest can be in a single list

		private string currentLine = "";
		public bool fixBuggedPlayers;
		private Process gameProcess;

		private int logID;
		public bool nolog;
		public int printSpeed = 150;
		public bool runOptimized = true;

		private string session_id;
		private bool stopping;

		public Server(string serverDir, string configKey, Config multiAdminCfg, string mainConfigLocation, string configChain, bool multiMode)
		{
			this.multiMode = multiMode;
			MainConfigLocation = mainConfigLocation;
			ConfigKey = configKey;
			ConfigChain = configChain;
			ServerDir = serverDir;
			session_id = Utils.GetUnixTime().ToString();
			Commands = new Dictionary<string, ICommand>();
			Features = new List<Feature>();
			tick = new List<IEventTick>();
			MultiAdminCfg = multiAdminCfg;
			StartDateTime = Utils.GetDate();
			maLogLocation = LogFolder + StartDateTime + "_MA_output_log.txt";
			stopping = false;
			InitialRoundStarted = false;
			readerThread = new Thread(() => InputThread.Write(this));
			printerThread = new Thread(() => OutputThread.Read(this));
			// Enable / Disable MultiAdmin Optimizations
			runOptimized = multiAdminCfg.config.GetBool("enable_multiadmin_optimizations", true);
			printSpeed = multiAdminCfg.config.GetInt("multiadmin_print_speed", 150);
			nolog = multiAdminCfg.config.GetBool("multiadmin_nolog", false);

			// NEW COMMIT
			// Register all features 
			RegisterFeatures();
			// Load config 
			ServerConfig = multiMode ? new Config(ServerDir + Path.DirectorySeparatorChar + ConfigKey + Path.DirectorySeparatorChar + "config_gameplay.txt") : new Config(mainConfigLocation);
			// Init features
			InitFeatures();
			// Start the server and threads
			if (StartServer())
			{
				readerThread.Start();
				printerThread.Start();
				MainLoop();
			}
		}

		public bool HasServerMod { get; set; }
		public string ServerModVersion { get; set; }
		public string ServerModBuild { get; set; }
		public Config MultiAdminCfg { get; }

		public Config ServerConfig { get; }

		public string ConfigKey { get; }
		public string MainConfigLocation { get; }
		public string ConfigChain { get; }
		public string ServerDir { get; }
		public bool InitialRoundStarted { get; set; }

		public List<Feature> Features { get; }
		public Dictionary<string, ICommand> Commands { get; }
		public string StartDateTime { get; }

		public string LogFolder
		{
			get
			{
				string loc = multiMode ? "servers" + Path.DirectorySeparatorChar + ConfigKey + Path.DirectorySeparatorChar + "logs" + Path.DirectorySeparatorChar : "logs" + Path.DirectorySeparatorChar;
				if (!Directory.Exists(loc)) Directory.CreateDirectory(loc);

				return loc;
			}
		}

		private static IEnumerable<Type> GetTypesWithHelpAttribute(Type attribute)
		{
			return from assembly in AppDomain.CurrentDomain.GetAssemblies() from type in assembly.GetTypes() let attribs = type.GetCustomAttributes(attribute, false) where attribs.Length > 0 select type;
		}

		private void RegisterFeatures()
		{
			/*
			var assembly = GetTypesWithHelpAttribute(typeof(Feature)).ToList();
			foreach (Type type in assembly)
            {
                var feature = Activator.CreateInstance(type, this) as Feature;
                if(feature is null)
                    continue;

                RegisterFeature(feature);
            }
			*/
			RegisterFeature(new Autoscale(this));
			RegisterFeature(new ChainStart(this));
			RegisterFeature(new ConfigReload(this));
			RegisterFeature(new ExitCommand(this));
			//RegisterFeature(new EventTest(this));
			RegisterFeature(new GithubGenerator(this));
			RegisterFeature(new HelpCommand(this));
			RegisterFeature(new InactivityShutdown(this));
			RegisterFeature(new MemoryChecker(this));
			RegisterFeature(new MemoryCheckerSoft(this));
			RegisterFeature(new ModLog(this));
			RegisterFeature(new MultiAdminInfo(this));
			RegisterFeature(new NewCommand(this));
			RegisterFeature(new Restart(this));
			RegisterFeature(new RestartNextRound(this));
			RegisterFeature(new RestartRoundCounter(this));
			RegisterFeature(new StopNextRound(this));
			RegisterFeature(new Titlebar(this));
		}

		private void InitFeatures()
		{
			foreach (Feature feature in Features)
			{
				feature.Init();
				feature.OnConfigReload();
			}
		}

		public void MainLoop()
		{
			while (!stopping)
			{
				if (gameProcess != null && !gameProcess.HasExited)
				{
					foreach (IEventTick tickEvent in tick)
						tickEvent.OnTick();
				}
				else if (!stopping)
				{
					foreach (Feature f in Features)
						if (f is IEventCrash crash)
							crash.OnCrash();

					Write("Game engine exited/crashed/closed/restarting", ConsoleColor.Red);
					Write("Cleaning Session", ConsoleColor.Red);
					DeleteSession();
					//session_id = Utils.GetUnixTime().ToString();
					Write("Restarting game with new session id");
					StartServer();
					InitFeatures();
				}

				Thread.Sleep(1000);
			}

			Thread.Sleep(100);
			CleanUp();
		}

		public bool IsStopping()
		{
			return stopping;
		}

		public void RegisterFeature(Feature feature)
		{
			if (feature is IEventTick eventTick) tick.Add(eventTick);
			if (feature is ICommand command) Commands.Add(command.GetCommand().ToLower().Trim(), command);

			Features.Add(feature);
		}


		public void PrepareFiles()
		{
			try
			{
				Directory.CreateDirectory("SCPSL_Data" + Path.DirectorySeparatorChar + "Dedicated" + Path.DirectorySeparatorChar + session_id);
				Write("Started new session.", ConsoleColor.DarkGreen);
			}
			catch
			{
				Write("Failed - Please close all open files in SCPSL_Data/Dedicated and restart the server!", ConsoleColor.Red);
				Write("Press any key to close...", ConsoleColor.DarkGray);
				Console.ReadKey(true);
				Process.GetCurrentProcess().Kill();
			}
		}

		public void Write(string message, ConsoleColor color = ConsoleColor.Yellow, int height = 0)
		{
			Log(message);
			if (SkipProcessHandle() || Process.GetCurrentProcess().MainWindowHandle != IntPtr.Zero)
			{
				int cursorTop = 0, bufferHeight = 0;
				try
				{
					cursorTop = Console.CursorTop + height;
					bufferHeight = Console.BufferHeight;
					if (cursorTop < 0)
						cursorTop = 0;
					else if (cursorTop >= Console.BufferHeight) cursorTop = Console.BufferHeight - 1;
					Console.CursorTop = cursorTop;
					Console.ForegroundColor = color;
					message = Timestamp(message);
					Console.WriteLine(message);
					Console.ForegroundColor = ConsoleColor.White;
					Console.BackgroundColor = ConsoleColor.Black;
				}
				catch (ArgumentOutOfRangeException e)
				{
					Console.WriteLine(Timestamp("Value " + cursorTop + " exceeded buffer height " + bufferHeight + "."));
					Console.WriteLine(e.StackTrace);
				}
			}
		}

		public static bool SkipProcessHandle()
		{
			int p = (int) Environment.OSVersion.Platform;
			return p == 4 || p == 6 || p == 128; // Outputs true for Unix
		}

		public void WritePart(string part, ConsoleColor backgroundColor = ConsoleColor.Black, ConsoleColor textColor = ConsoleColor.Yellow, bool date = false, bool lineEnd = false)
		{
			string datepart = "";
			if (date)
			{
				DateTime now = DateTime.Now;
				datepart = "[" + now.Hour.ToString("00") + ":" + now.Minute.ToString("00") + ":" + now.Second.ToString("00") + "] ";
			}

			Console.ForegroundColor = textColor;
			Console.BackgroundColor = backgroundColor;
			if (lineEnd)
			{
				if (part.EndsWith(Environment.NewLine))
				{
					//this.Write("This ends in a newline, not adding anymore!");
					Console.Write(datepart + part);
				}
				else
					Console.Write(datepart + part + Environment.NewLine);

				currentLine += datepart + part;
				Log(currentLine);
				currentLine = "";
			}
			else
			{
				Console.Write(datepart + part);
				currentLine += datepart + part;
			}
		}

		public void Log(string message)
		{
			lock (this)
			{
				using (StreamWriter sw = File.AppendText(maLogLocation))
				{
					message = Timestamp(message);
					sw.WriteLine(message);
				}
			}
		}

		public void SoftRestartServer()
		{
			if (ServerModCheck(1, 5, 0))
			{
				SendMessage("RESTART");
				//session_id = Utils.GetUnixTime().ToString();
			}
			else
				gameProcess.Kill();
		}

		public bool ServerModCheck(int major, int minor, int fix)
		{
			if (ServerModVersion == null) return false;

			string[] parts = ServerModVersion.Split('.');
			int verMajor = 0;
			int verMinor = 0;
			int verFix = 0;
			if (parts.Length == 3)
			{
				int.TryParse(parts[0], out verMajor);
				int.TryParse(parts[1], out verMinor);
				int.TryParse(parts[2], out verFix);
			}
			else if (parts.Length == 2)
			{
				int.TryParse(parts[0], out verMajor);
				int.TryParse(parts[1], out verMinor);
			}
			else
				return false;

			if (major == 0 && minor == 0 && verFix == 0) return false;

			return verMajor > major || verMajor >= major && verMinor > minor || verMajor >= major && verMinor >= minor && verFix >= fix;
		}

		public void RestartServer()
		{
			gameProcess.Kill();
			string file = File.Exists("MultiAdmin.exe") ? "MultiAdmin.exe" : "MultiAdmin";
			ProcessStartInfo psi = new ProcessStartInfo(file, ConfigKey)
			{
				UseShellExecute = true
			};
			Process.Start(psi);
			stopping = true;
		}

		public void StopServer(bool killGame = true)
		{
			foreach (Feature f in Features)
				if (f is IEventServerStop stop)
					stop.OnServerStop();

			if (killGame) gameProcess.Kill();
			stopping = true;
		}

		public void CleanUp()
		{
			RemoveRunFile();
			DeleteSession();
		}


		public bool StartServer()
		{
			bool started = false;
			InitialRoundStarted = false;
			try
			{
				string configdir = "servers" + Path.DirectorySeparatorChar + ConfigKey + Path.DirectorySeparatorChar;
				PrepareFiles();
				string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "SCPSL.*", SearchOption.TopDirectoryOnly);
				if (multiMode && !File.Exists(configdir + "config_gameplay.txt") && File.Exists(configdir + "config.txt")) File.Copy(configdir + "config.txt", configdir + "config_gameplay.txt");
				if (multiMode && !File.Exists(configdir + "config_remoteadmin.txt") && File.Exists(FileManager.AppFolder + "config_remoteadmin.txt")) File.Copy(FileManager.AppFolder + "config_remoteadmin.txt", configdir + "config_remoteadmin.txt");
				//SwapConfigs();
				string dedicatedDir = "SCPSL_Data" + Path.DirectorySeparatorChar + "Dedicated" + Path.DirectorySeparatorChar + session_id;
				//if (Directory.Exists(dedicatedDir)) Directory.Delete(dedicatedDir, true);
				Write("Executing: " + files[0], ConsoleColor.DarkGreen);
				string args = nolog ? "-batchmode -nographics -key" + session_id + " -silent-crashes -id" + Process.GetCurrentProcess().Id + " -nolog" : "-batchmode -nographics -key" + session_id + " -silent-crashes -id" + Process.GetCurrentProcess().Id + " -logFile \"" + LogFolder + Utils.GetDate() + "_SCP_output_log.txt" + "\"";
				if (multiMode) args += " -configpath " + '"' + "servers" + Path.DirectorySeparatorChar + ConfigKey + '"' + " -sharenonconfigs";
				Write("Starting server with the following parameters");
				Write(files[0] + " " + args);
				ProcessStartInfo startInfo = new ProcessStartInfo(files[0]) {Arguments = args};
				gameProcess = Process.Start(startInfo);
				CreateRunFile();
				started = true;
				foreach (Feature f in Features)
					if (f is IEventServerPreStart start)
						start.OnServerPreStart();
			}
			catch (Exception e)
			{
				Write("Failed - Executable file not found or config issue!", ConsoleColor.Red);
				Write(e.Message, ConsoleColor.Red);
				Write("Press any key to close...", ConsoleColor.DarkGray);
				RemoveRunFile();
				Console.ReadKey(true);
				Process.GetCurrentProcess().Kill();
			}

			return started;
		}

		public Process GetGameProccess()
		{
			return gameProcess;
		}

		/*public void SwapConfigs()
		{
			if (multiMode)
			{
				if (File.Exists("servers" + Path.DirectorySeparatorChar + ConfigKey + Path.DirectorySeparatorChar + "config.txt"))
				{
					string contents = File.ReadAllText(Utils.GetParentDir()  + Path.DirectorySeparatorChar + "servers" + Path.DirectorySeparatorChar + ConfigKey + Path.DirectorySeparatorChar + "config.txt");
					File.WriteAllText(MainConfigLocation, contents);

					string confignames = "config.txt";

					DirectoryInfo dir = new DirectoryInfo(Utils.GetParentDir()  + Path.DirectorySeparatorChar + "servers" + Path.DirectorySeparatorChar + ConfigKey + Path.DirectorySeparatorChar);
					foreach (FileInfo file in dir.GetFiles().Where(file => file.Name.Contains("config_")))
					{
						contents = File.ReadAllText(file.FullName);
						File.WriteAllText(MainConfigLocation.Replace("config_gameplay.txt", file.Name), contents);
						confignames += ", " + file.Name;
					}

					Write("Config file swapped: " + confignames, ConsoleColor.DarkYellow);
				}
				else
				{
					Write("Config file for server " + ConfigKey + " does not exist! expected location:" + "servers\\" + ConfigKey + "\\config.txt", ConsoleColor.DarkYellow);
					throw new FileNotFoundException("config file not found");
				}
			}
		}*/

		private void RemoveRunFile()
		{
			if (multiMode) File.Delete(ServerDir + Path.DirectorySeparatorChar + ConfigKey + Path.DirectorySeparatorChar + "running");
		}

		private void CreateRunFile()
		{
			if (multiMode) File.Create(ServerDir + Path.DirectorySeparatorChar + ConfigKey + Path.DirectorySeparatorChar + "running").Close();
		}

		private void CleanSession()
		{
			string path = "SCPSL_Data" + Path.DirectorySeparatorChar + "Dedicated" + Path.DirectorySeparatorChar + session_id;
			if (Directory.Exists(path))
			{
				foreach (string file in Directory.GetFiles(path))
					File.Delete(file);
			}
		}

		private void DeleteSession()
		{
			CleanSession();
			//string path = "SCPSL_Data" + Path.DirectorySeparatorChar + "Dedicated" + Path.DirectorySeparatorChar + session_id;
			//if (Directory.Exists(path)) Directory.Delete(path);
		}


		public string GetSessionId()
		{
			return session_id;
		}

		public bool IsConfigRunning(string config)
		{
			return File.Exists(ServerDir + Path.DirectorySeparatorChar + config + Path.DirectorySeparatorChar + "running");
		}

		public void NewInstance(string configChain)
		{
			string file = File.Exists("MultiAdmin.exe") ? "MultiAdmin.exe" : "MultiAdmin";
			ProcessStartInfo psi = new ProcessStartInfo(file, configChain)
			{
				UseShellExecute = true
			};
			Process.Start(psi);
		}

		public void SendMessage(string message)
		{
			string sessionDirectory = "SCPSL_Data" + Path.DirectorySeparatorChar + "Dedicated" + Path.DirectorySeparatorChar + session_id;
			if (!Directory.Exists(sessionDirectory))
			{
				Write("Send Message error: sending " + message + " failed. " + sessionDirectory + " does not exist!", ConsoleColor.Yellow);
				Write("skipping");
				return;
			}

			string file = sessionDirectory + Path.DirectorySeparatorChar + "cs" + logID + ".mapi";
			if (File.Exists(file))
			{
				Write("Send Message error: sending " + message + " failed. " + file + " already exists!", ConsoleColor.Yellow);
				Write("skipping");
				logID++;
				return;
			}

			StreamWriter streamWriter = new StreamWriter(file);
			logID++;
			streamWriter.WriteLine(message + "terminator");
			streamWriter.Close();
			Write("Sending request to SCP: Secret Laboratory...", ConsoleColor.White);
		}

		public static string Timestamp(string message)
		{
			if (string.IsNullOrEmpty(message))
				return string.Empty;
			DateTime now = DateTime.Now;
			message = "[" + now.Hour.ToString("00") + ":" + now.Minute.ToString("00") + ":" + now.Second.ToString("00") + "] " + message;
			return message;
		}
	}
}