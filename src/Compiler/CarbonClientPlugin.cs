using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Carbon.Client.API;
using Carbon.Client.Base;
using UnityEngine;

/*
 *
 * Copyright (c) 2023 Carbon Community 
 * All rights reserved.
 *
 */

namespace Carbon.Client;

public abstract class CarbonClientPlugin : FacepunchBehaviour
{
	internal static Dictionary<string, CarbonClientPlugin> Plugins = new();

	internal Dictionary<string, Func<object[], object>> _hooks = new();
	internal Type _pluginType;
	internal const BindingFlags _flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

	public static void UnloadAll()
	{
		var temp = Plugins.Values.ToArray();

		foreach (var plugin in temp)
		{
			plugin.IUnload(true);
		}

		Array.Clear(temp, 0, temp.Length);
		temp = null;
	}

	public Info Info { get; set; }

	public void Log(object message)
	{
		Debug.Log($"[{Info.Name}] {message}");
	}

	public object CallHook(string hook, object[] args)
	{
		if(_hooks.TryGetValue(hook, out var func))
		{
			return func(args);
		}

		return default;
	}
	public T GetComponentImpl<T>() where T : Component
	{
		return GetComponent<T>();
	}

	public abstract void OnInit();
	public abstract void OnUninit();

	public override string ToString()
	{
		return $"{Info.Name}";
	}

	internal void ILoad()
	{
		IRefreshHooks();
		ICommandInstall();
	}
	internal void IUnload(bool clear = false)
	{
		CommandLoader.UnregisterType(this);
		BaseHook.UnsubscribePlugin(this);

		Console.WriteLine($"Unloaded plugin {Info.Name}");

		if (clear) Plugins.Remove(Info.Name);

		try
		{
			OnUninit();
		}
		catch (Exception ex) { Log($"Failed Unload: {ex}"); }

		try
		{
			// Destroy(gameObject);
			DestroyImmediate(this);
		}
		catch { }
	}

	internal void IRefreshHooks()
	{
		_hooks.Clear();

		foreach (var method in _pluginType.GetMethods(_flags))
		{
			var name = method.Name;

			if (BaseHook.Exists(name, out var hook))
			{
				_hooks.Add(name, args => method.Invoke(this, args));
				hook.Subscribe(this);
			}
		}
	}
	internal void ICommandInstall()
	{
		CommandLoader.RegisterType(_pluginType, this);
	}
}
