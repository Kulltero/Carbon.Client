using System;
using Carbon.Client.Base;

namespace Carbon.Client.API;

public class HookCaller
{
	public static object CallHook(string name, params object[] args)
	{
		if (BaseHook._cache.TryGetValue(name, out var hook))
		{
			var result = hook.Call(args);
			Array.Clear(args, 0, args.Length);
			args = null;
			return result;
		}

		return null;
	}
}
