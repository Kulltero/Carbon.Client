using System;
using Carbon.Client.API;
using Carbon.Client.Base;
using HarmonyLib;
using Network;

namespace Carbon.Client.Hooks;

[HarmonyPatch(typeof(BaseEntity), "OnRpcMessage", new Type[] { typeof(BasePlayer), typeof(uint), typeof(Message) })]
[Hook("OnRPCMessage")]
public class OnRPCMessage
{
	public static bool Prefix(BasePlayer player, uint rpc, Message msg, BaseEntity __instance, ref bool __result)
	{
		if (HookCaller.CallHook("OnRPCMessage", __instance, player, rpc, msg) is bool hookValue)
		{
			__result = hookValue;
			return true;
		}

		return false;
	}
}
