using System;
using System.IO;

namespace MultiAdmin.MultiAdmin
{
	internal class Utils
	{
		public static string GetDate()
		{
			return DateTime.Now.ToString("yyyy-MM-dd_HH_mm");
		}

		// this is a legacy method since there is no proper unix time method before net framework 4.6 :(
		public static long GetUnixTime()
		{
			TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
			return (long) t.TotalSeconds;
		}

		public static string GetParentDir()
		{
			return Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
		}
	}
}