using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Carbon.Client.Base;
using Carbon.Extensions;

namespace Carbon.Client.Base
{
	public class BaseCommand
	{
		public CarbonClientPlugin Plugin { get; set; }

		public string Name { get; set; }

		public Func<Arg, string> Callback { get; set; }
	}
}

public struct Arg : IDisposable
{
	public BasePlayer Player { get; set; }
	public string Name { get; set; }
	public string[] Args { get; set; }
	public BaseCommand Command { get; set; }

	public string GetSentence(string separator = " ", string lastSeparator = " ")
	{
		return Args.ToString(separator, lastSeparator);
	}
	public string GetString(int arg, string def = null)
	{
		if (arg > Args.Length - 1)
		{
			return def;
		}

		return Args[arg];
	}
	public int GetInt(int arg, int def = 0)
	{
		if (int.TryParse(GetString(arg), out var value))
		{
			return value;
		}

		return def;
	}
	public float GetFloat(int arg, float def = 0)
	{
		if (float.TryParse(GetString(arg), out var value))
		{
			return value;
		}

		return def;
	}
	public ulong GetULong(int arg, ulong def = 0)
	{
		if (ulong.TryParse(GetString(arg), out var value))
		{
			return value;
		}

		return def;
	}

	public void Dispose()
	{
		Array.Clear(Args, 0, Args.Length);
		Args = null;
	}
}
