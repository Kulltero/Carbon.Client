using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Carbon.Client.API;
using HarmonyLib;
using Network.Visibility;
using UnityEngine;

namespace Carbon.Client.Base;

public class BaseHook : IDisposable
{
	public static Dictionary<string, BaseHook> _cache = new();

	public string Id { get; set; }
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
					Id = type.Name.Split('_')[2],
					Name = hook.Name,
					Category = category.Name,
					_type = type,
					_hook = hook,
					_harmony = type.GetCustomAttribute<HarmonyPatch>()
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
	internal Harmony _patch;
	internal Type _type;
	internal HarmonyPatch _harmony;
	internal HookAttribute _hook;
	internal const string _prefix = "Prefix";
	internal const string _postfix = "Postfix";
	internal const BindingFlags _flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic;
	internal static List<CarbonClientPlugin> _pluginBuffer = new();

	public object Call(object[] args)
	{
		var result = (object)null;

		_pluginBuffer.Clear();
		_pluginBuffer.AddRange(_subscribers);

		foreach (var subscriber in _pluginBuffer)
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

	public void Patch()
	{
		if (_patch != null) return;

		_patch = new Harmony($"com.carbon.client_hooks.{Guid.NewGuid():N}");

		try
		{
			var prefixMethod = _type.GetMethod(_prefix, _flags);
			var postfixMethod = _type.GetMethod(_postfix, _flags);

			var prefix = (HarmonyMethod)null;
			var postfix = (HarmonyMethod)null;

			if (prefixMethod != null) prefix = new HarmonyMethod(prefixMethod);
			if (postfixMethod != null) postfix = new HarmonyMethod(postfixMethod);

			_patch.Patch(_harmony.info.declaringType.GetMethod(_harmony.info.methodName, _flags, _harmony.info.argumentTypes), prefix, postfix, null);
            UnityEngine.Debug.Log($"Patched hook '{Name}' ({_harmony.info.declaringType.FullName}.{_harmony.info.methodName})");
		}
		catch (Exception ex)
		{
            UnityEngine.Debug.LogWarning($"Failed patching hook '{Name}': {_type.Name} ({ex.Message})\n{ex.StackTrace}");
		}
	}
	public void Unpatch()
	{
		if (_patch == null) return;

		_patch.UnpatchSelf();
		_patch = null;
	}

	public void Subscribe(CarbonClientPlugin plugin)
	{
		if (_subscribers.Contains(plugin)) return;

		_subscribers.Add(plugin);

		if (plugin.Info != null)
		{
			Console.WriteLine($"Subscribed to '{Name}'");
		}

		Patch();
	}
	public void Unsubscribe(CarbonClientPlugin plugin)
	{
		if (!_subscribers.Contains(plugin)) return;

		_subscribers.Remove(plugin);

		if (plugin.Info != null)
		{
			Console.WriteLine($"Unsubscribed from '{Name}'");
		}

		if(_subscribers.Count == 0)
		{
			Unpatch();
		}
	}
	public void UnsubscribeAll()
	{
		_subscribers.Clear();
		_subscribers = null;

		Unpatch();
	}

	public void Dispose()
	{
		UnsubscribeAll();
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
