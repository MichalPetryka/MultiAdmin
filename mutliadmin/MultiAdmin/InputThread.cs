﻿using System;
using System.Linq;
using System.Threading;

namespace MultiAdmin.MultiAdmin
{
	internal class InputThread
	{
		public static void Write(Server server)
		{
			while (!server.IsStopping())
			{
				while (!Console.KeyAvailable)
				{
					if (server.IsStopping()) return;
					Thread.Sleep(server.runOptimized ? 500 : 300);
				}

				string message = Console.ReadLine();
				int cursorTop = Console.CursorTop;
				Console.SetCursorPosition(0, Console.CursorTop - 1);
				Console.Write(new string(' ', Console.WindowWidth));
				server.Write(">>> " + message, ConsoleColor.DarkMagenta, -1);
				Console.SetCursorPosition(0, cursorTop);
				if (message != null)
				{
					string[] messageSplit = message.ToUpper().Split(' ');
					if (messageSplit.Length > 0)
					{
						bool callServer = true;
						server.Commands.TryGetValue(messageSplit[0].ToLower().Trim(), out ICommand command);
						if (command != null)
						{
							command.OnCall(messageSplit.Skip(1).Take(messageSplit.Length - 1).ToArray());
							callServer = command.PassToGame();
						}

						if (callServer) server.SendMessage(message);
					}
				}
			}
		}
	}
}