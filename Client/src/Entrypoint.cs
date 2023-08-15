using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;
using Carbon.Client;
using Carbon.Client.API;
using Carbon.Client.Contracts;
using Carbon.Client.Core;
using Carbon.Client.Packets;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Network;
using Newtonsoft.Json;
using ProtoBuf;
using UnityEngine;

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
	internal static string _home = Path.Combine(Application.dataPath, "..");
	internal static bool _serverConnected;
	internal static Persistence _persistence;

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
				UnityEngine.Debug.Log($"Booting Carbon client...");

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

				UnityEngine.Debug.Log($"Initializing compiler...");
				_persistence = IL2CPPChainloader.AddUnityComponent<Persistence>();
				_persistence.StartCoroutine(CompilerHelper.CompileRoutine());
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogError($"Failed CarbonCommunityEntity init ({ex.Message})\n{ex.StackTrace}");
			}
		}
	}

	[HarmonyPatch(typeof(CommunityEntity), "OnRpcMessage", new System.Type[] { typeof(BasePlayer), typeof(uint), typeof(Message) })]
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

	public static bool Send<T>(RPC rpc, T packet) where T : IPacket
	{
		if (!_serverConnected) return false;

		try
		{
			CommunityEntity.ClientInstance.ServerRPC(rpc.Name, JsonConvert.SerializeObject(packet.Serialize()));
			// return true;
			// 
			// var write = Net.cl.StartWrite();
			// write.PacketID(Message.Type.RPCMessage);
			// write.EntityID(CommunityEntity.ClientInstance.net.ID);
			// write.UInt32(rpc.Id);
			// write.BytesWithSize(packet.Serialize());
			// write.Send(new SendInfo(Net.cl.Connection)
			// {
			// 	priority = Network.Priority.Immediate
			// });
		}
		catch (Exception ex)
		{
			ex = ex.Demystify();
			UnityEngine.Debug.LogError($"Failed sending server RPC ({ex.Message})\n{ex.StackTrace}");
			return false;
		}

		return true;
	}
	public static T Receive<T>(Network.Message message)
	{
		using var ms = new MemoryStream(JsonConvert.DeserializeObject<byte[]>(message.read.StringRaw()));
		var array = ms.ToArray();
		return Serializer.Deserialize<T>(new ReadOnlySpan<byte>(array, 0, array.Length));

		// using var ms = new MemoryStream(JsonConvert.DeserializeObject<byte[]>(message.read.StringRaw()));
		// return Serializer.Deserialize<T>(new ReadOnlySpan<byte>(ms.ToArray()));
	}


	[RPC.Method("ping")]
	private static void Ping(BasePlayer player, Network.Message message)
	{
		_serverConnected = true;

		var result = Receive<RPCList>(message);
		result.Sync();

		Send(RPC.Get("pong"), RPCList.Get());
	}

	[RPC.Method("clientinfo")]
	private static void ClientInfo(BasePlayer player, Network.Message message)
	{
		Send(RPC.Get("clientinfo"), new ClientInfo()
		{
			ScreenWidth = Screen.width,
			ScreenHeight = Screen.height
		});
	}
}
