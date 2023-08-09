using System;
using HarmonyLib;
using UnityEngine;

namespace Carbon.Client.Hooks;

[HarmonyPatch(typeof(MainMenuSystem), "Show", new Type[] { })]
public class OnMenuShow
{
	public async static void Prefix()
	{
		Debug.Log($"Show!");
	}
}

[HarmonyPatch(typeof(MainMenuSystem), "Hide", new Type[] { })]
public class OnMenuHide
{
	public async static void Prefix()
	{
		Debug.Log($"Hide!");
	}
}
