using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

public class YamlConfig
{
	public string[] RawData;

	public YamlConfig()
	{
	}

	public YamlConfig(string path)
	{
		LoadConfigFile(path);
	}

	public void LoadConfigFile(string path)
	{
		RawData = File.Exists(path) ? Filter(FileManager.ReadAllLines(path)) : new string[] { };
	}

	private static string[] Filter(IEnumerable<string> lines)
	{
		return lines.Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith("#") && (line.StartsWith(" - ") || line.Contains(':'))).ToArray();
	}

	private string GetRawString(string key, string def)
	{
		foreach (string line in RawData)
			if (line.StartsWith(key + ": "))
				return line.Substring(key.Length + 2) == "default" ? def : line.Substring(key.Length + 2);

		return def;
	}


	public string GetString(string key, string def = "")
	{
		return GetRawString(key, def);
	}

	public int GetInt(string key, int def = 0)
	{
		return int.TryParse(GetRawString(key, def.ToString()), NumberStyles.Any, CultureInfo.InvariantCulture, out int temp) ? temp : def;
	}

	public float GetFloat(string key, float def = 0)
	{
		return float.TryParse(GetRawString(key, def.ToString(CultureInfo.InvariantCulture)).Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out float temp) ? temp : def;
	}

	public bool GetBool(string key, bool def = false)
	{
		return bool.TryParse(GetRawString(key, def.ToString()).ToLower(), out bool temp) ? temp : def;
	}


	public List<string> GetStringList(string key)
	{
		bool read = false;
		List<string> list = new List<string>();
		foreach (string line in RawData)
		{
			if (line.StartsWith(key) && line.EndsWith("[]")) break;
			if (line.StartsWith(key + ":"))
			{
				read = true;
				continue;
			}

			if (!read) continue;
			if (line.StartsWith(" - ")) list.Add(line.Substring(3));
			else if (!line.StartsWith("#")) break;
		}

		return list;
	}


	public List<int> GetIntList(string key)
	{
		List<string> list = GetStringList(key);
		return list.Select(x => Convert.ToInt32(x)).ToList();
	}

	public Dictionary<string, string> GetStringDictionary(string key)
	{
		List<string> list = GetStringList(key);
		Dictionary<string, string> dict = new Dictionary<string, string>();
		foreach (string item in list)
		{
			int i = item.IndexOf(": ", StringComparison.Ordinal);
			dict.Add(item.Substring(0, i), item.Substring(i + 2));
		}

		return dict;
	}

	public static string[] ParseCommaSeparatedString(string data)
	{
		if (!data.StartsWith("[") || !data.EndsWith("]")) return null;
		data = data.Substring(1, data.Length - 2);
		return data.Split(new[] {", "}, StringSplitOptions.None);
	}
}