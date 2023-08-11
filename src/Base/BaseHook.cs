using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Carbon.Client.Base;

public class BaseHook : IDisposable
{
	public static Dictionary<string, BaseHook> _cache = new();

	public string Name { get; set; }
	public string Category { get; set; }

	public static bool Exists(string name, out BaseHook hook)
	{
		return _cache.TryGetValue(name, out hook);
	}
	public static void UnsubscribePlugin(CarbonClientPlugin plugin)
	{
		foreach (var hook in _cache.Values)
		{
			hook.Unsubscribe(plugin);
		}
	}

	public static void Rebuild(params Assembly[] assemblies)
	{
		foreach (var hook in _cache.Values)
		{
			hook.Dispose();
		}

		_cache.Clear();

		foreach (var assembly in assemblies)
		{
			foreach (var type in assembly.GetTypes())
			{
				var hook = type.GetCustomAttribute<HookAttribute>();
				var category = type.GetCustomAttribute<HookCategory>();
				if (hook == null) continue;

				_cache.Add(hook.Name, new BaseHook
				{
					Name = hook.Name,
					Category = category.Name
				});

				Console.WriteLine($"Installed hook [{category.Name}] {hook.Name}");
			}
		}

		foreach(var plugin in CarbonClientPlugin.Plugins)
		{
			plugin.Value.IRefreshHooks();
		}
	}

	internal List<CarbonClientPlugin> _subscribers = new();

	public object Call(object[] args)
	{
		var result = (object)null;

		foreach (var subscriber in _subscribers)
		{
			try
			{
				var currentResult = subscriber.CallHook(Name, args);

				if (currentResult != null && result == null)
				{
					result = currentResult;
				}
			}
			catch (Exception ex)
			{
				ex = (ex.InnerException ?? ex).Demystify();
                UnityEngine.Debug.LogError($"Failed calling hook '{Name}' {(subscriber.Info != null ? $"on {subscriber.Info?.Name}" : string.Empty)}({ex.Message})\n{ex.StackTrace}");
			}
		}

		return result;
	}

	public void Subscribe(CarbonClientPlugin plugin)
	{
		if (_subscribers.Contains(plugin)) return;

		_subscribers.Add(plugin);

		if (plugin.Info != null)
		{
			Console.WriteLine($"Subscribed to '{Name}'");
		}
	}
	public void Unsubscribe(CarbonClientPlugin plugin)
	{
		if (!_subscribers.Contains(plugin)) return;

		_subscribers.Remove(plugin);

		if (plugin.Info != null)
		{
			Console.WriteLine($"Unsubscribed from '{Name}'");
		}
	}
	public void UnsubscribeAll()
	{
		_subscribers.Clear();
	}

	public void Dispose()
	{

	}
}

[AttributeUsage(AttributeTargets.Class)]
public class HookAttribute : Attribute
{
	public string Name { get; set; }

	public HookAttribute(string name)
	{
		Name = name;
	}
}


[AttributeUsage(AttributeTargets.Class)]
public class HookCategory : Attribute
{
	public string Name { get; set; }

	public HookCategory(string name)
	{
		Name = name;
	}
}
