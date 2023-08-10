using System;
using System.IO;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;
using Carbon.Client;
using Carbon.Client.API;
using Carbon.Client.Base;
using HarmonyLib;
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

				HookLoader.Reload();
				BaseHook.Rebuild();

				await References.Load();

				CarbonCommunityEntity.Init();

				// var ent = new GameObject(CarbonCommunityEntity.PrefabName).AddComponent<CarbonCommunityEntity>();
				// ent.prefabID = CarbonCommunityEntity.PrefabId;
				// ent._prefabName = CarbonCommunityEntity.PrefabName;
				// FileSystem.Backend.cache.TryAdd(CarbonCommunityEntity.PrefabName, ent.gameObject);

				Debug.Log($"Initializing compiler...");
				IL2CPPChainloader.AddUnityComponent<Persistence>().StartCoroutine(CompilerHelper.CompileRoutine());
			}
			catch (Exception ex)
			{
				Debug.LogError($"Failed CarbonCommunityEntity init ({ex.Message})\n{ex.StackTrace}");
			}
		}
	}
}
