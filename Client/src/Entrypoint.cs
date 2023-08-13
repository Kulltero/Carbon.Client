using System;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;
using Carbon.Client;
using Carbon.Client.API;
using Carbon.Client.Base;
using Carbon.Client.Core;
using Carbon.Client.Packets;
using HarmonyLib;
using Il2CppSystem.Runtime.Remoting;
using Network;
using UnityEngine;
using static Carbon.Client.RPC;

/*
 *
 * Copyright (c) 2023 Carbon Community 
 * All rights reserved.
 *
 */

namespace Carbon;

#region Pragmas
#pragma warning disable CS8618
#endregion

[BepInPlugin("418cad37fcc041e1b25c5a59320f7e58", "CarbonCommunity.Client", "1.0.0")]
public class Entrypoint : BasePlugin
{
	public static Entrypoint Singleton { get; internal set; }

	internal static bool _hasInit;
	internal static string Home = Path.Combine(Application.dataPath, "..");

	public override void Load()
	{
		Singleton = this;

		var harmony = new Harmony("com.carboncommunity.client");
		harmony.PatchAll();

		CompilerHelper.PluginsFolder = System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "BepInEx", "carbon");
		if (!Directory.Exists(CompilerHelper.PluginsFolder)) Directory.CreateDirectory(CompilerHelper.PluginsFolder);
	}

	public class Persistence : FacepunchBehaviour
	{

	}

	[HarmonyPatch(typeof(MainMenuSystem), "Awake", new Type[] { })]
	public class Initial
	{
		public async static void Prefix()
		{
			if (_hasInit) return;
			_hasInit = true;
			try
			{
				Debug.Log($"Booting Carbon client...");

				RPC.Init(typeof(RPC), typeof(Entrypoint));
				HookLoader.Reload();
				HookLoader.Patch();

				await References.Load();

  				// CarbonCommunityEntity.Init();
				// 
				// var ent = new GameObject(CarbonCommunityEntity.PrefabName).AddComponent(Il2CppInterop.Runtime.Il2CppType.From(typeof(CarbonCommunityEntity))).Cast<CarbonCommunityEntity>();
				// ent.prefabID = CarbonCommunityEntity.PrefabId;
				// ent._prefabName = CarbonCommunityEntity.PrefabName;
				// if (!FileSystem.Backend.cache.ContainsKey(CarbonCommunityEntity.PrefabName)) FileSystem.Backend.cache.Add(CarbonCommunityEntity.PrefabName, ent.gameObject);

				var corePlugin = new CorePlugin()
				{
					_pluginType = typeof(CorePlugin)
				};
				corePlugin.ILoad();
				corePlugin.OnInit();
				CarbonClientPlugin.Plugins.Add("Core", corePlugin);

				Debug.Log($"Initializing compiler...");
				IL2CPPChainloader.AddUnityComponent<Persistence>().StartCoroutine(CompilerHelper.CompileRoutine());
			}
			catch (Exception ex)
			{
				Debug.LogError($"Failed CarbonCommunityEntity init ({ex.Message})\n{ex.StackTrace}");
			}
		}
	}

	[HarmonyPatch( typeof(CommunityEntity), "OnRpcMessage", new System.Type[] { typeof(BasePlayer), typeof(uint), typeof(Message) })]
	public class CommunityEntityPatch
	{
		private static bool Prefix(BasePlayer player, uint rpc, Message msg, CommunityEntity __instance, ref bool __result)
		{
			var rpcVal = RPC.Get(rpc);
			Console.WriteLine($"{player} {rpc} {rpcVal.Name} {msg} {__instance}");

			if (RPC.HandleRPCMessage(player, rpc, msg) is bool value)
			{
				__result = value;
				return false;
			}
			return true;
		}
	}

	[Method("ping")]
	private static void Ping(BasePlayer player, Network.Message message)
	{
		var packet = new ServerRPCList
		{
			RpcNames = rpcList.Select(x => x.Name).ToArray()
		};

		var rpc = Get("pong");
		Console.WriteLine($"{rpc.Name} {rpc.Id} {packet.RpcNames.Length}");
		CommunityEntity.ClientInstance.ServerRPC(SendMethod.Reliable, rpc.Name);
	}
}
