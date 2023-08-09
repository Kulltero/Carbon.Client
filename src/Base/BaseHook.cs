using System;
using System.Collections.Generic;
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

	public static void Rebuild()
	{
		foreach (var hook in _cache.Values)
		{
			hook.Dispose();
		}

		_cache.Clear();

		var assembly = typeof(BaseHook).Assembly;
		foreach (var type in assembly.GetTypes())
		{
			var attribute = type.GetCustomAttribute<HookAttribute>();
			if (attribute == null) continue;

			_cache.Add(attribute.Name, new BaseHook
			{
				Name = attribute.Name,
			});

			Debug.Log($"Installed hook: {attribute.Name}");
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
				Debug.LogError($"Failed calling hook '{Name}' on {subscriber.Info.Name} ({ex.Message})\n{ex.StackTrace}");
			}
		}

		return result;
	}

	public void Subscribe(CarbonClientPlugin plugin)
	{
		if (_subscribers.Contains(plugin)) return;

		_subscribers.Add(plugin);
	}
	public void Unsubscribe(CarbonClientPlugin plugin)
	{
		if (!_subscribers.Contains(plugin)) return;

		_subscribers.Remove(plugin);
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
