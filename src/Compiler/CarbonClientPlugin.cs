using System;
using System.Collections.Generic;
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

	public Info Info { get; set; }

	public void Log(object message)
	{
		Debug.Log($"[{Info.Name}] {message}");
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

	internal void Unload(bool clear = false)
	{
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
}
