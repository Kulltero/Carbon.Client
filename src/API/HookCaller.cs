using Carbon.Client.Base;

namespace Carbon.Client.API;

public class HookCaller
{
	public static object CallHook(string name, params object[] args)
	{
		if (BaseHook._cache.TryGetValue(name, out var hook))
		{
			return hook.Call(args);
		}

		return null;
	}
}
