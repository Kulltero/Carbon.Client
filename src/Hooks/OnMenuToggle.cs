using System;
using Carbon.Client.API;
using Carbon.Client.Base;
using HarmonyLib;

namespace Carbon.Client.Hooks;

[HarmonyPatch(typeof(MainMenuSystem), "Show", new Type[] { })]
[Hook("OnMenuShow")]
public class OnMenuShow
{
	public static void Prefix()
	{
		HookCaller.CallHook("OnMenuShow", MainMenuSystem.Instance);
	}
}

[HarmonyPatch(typeof(MainMenuSystem), "Hide", new Type[] { })]
[Hook("OnMenuHide")]
public class OnMenuHide
{
	public static void Prefix()
	{
		HookCaller.CallHook("OnMenuHide", MainMenuSystem.Instance);
	}
}
