using System.IO;
using BepInEx;
using HarmonyLib;
using BepInEx.Unity.IL2CPP;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Unity.IL2CPP.Utils;
using Carbon.Base;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;
using System.Reflection;
using System.Linq;
using Il2CppSystem.Security.Cryptography;
using System.Text;
using System.Globalization;
using VLB;
using Carbon.Client;

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
		if (!Directory.Exists(CompilerHelper.PluginsFolder)) Directory.CreateDirectory(CompilerHelper.PluginsFolder );
	}

	[HarmonyPatch(typeof(global::Client), "NetworkInit")]
	public class MyClass_MyMethod_Patch
	{
		public static void Postfix(global::Client __instance)
		{
			if (_hasInit) return;
			_hasInit = true;

			References.Load(); 

			try
			{
				CarbonCommunityEntity.Init();

				var ent = new GameObject(CarbonCommunityEntity.PrefabName).AddComponent<CarbonCommunityEntity>();
				ent.prefabID = CarbonCommunityEntity.PrefabId;
				ent._prefabName = CarbonCommunityEntity.PrefabName;
				FileSystem.Backend.cache.TryAdd(CarbonCommunityEntity.PrefabName, ent.gameObject);
			}
			catch (Exception ex)
			{
				Debug.LogError($"Failed CarbonCommunityEntity init: {ex}");
			}

			Debug.Log($"Initializing compiler...");
			__instance.StartCoroutine(CompilerHelper.CompileRoutine());
		}
	}
}
