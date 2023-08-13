using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Carbon.Extensions;
using HarmonyLib;
using Il2CppSystem.IO;
using Il2CppSystem.Net;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Carbon.Client.API;

public class HookLoader
{
	public static Manifest CurrentManifest { get; internal set; }

	public const string HookUrl = "https://raw.githubusercontent.com/CarbonCommunity/Carbon.Redist/main/Client/hooks.json";
	public static string CurrentHookSource;

	public static Type FindType(string type)
	{
		return AccessTools.TypeByName(type);
	}

	public static string GetHookFile()
	{
		return System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "BepInEx", "plugins", "carbonc.json"));
	}

	public static void UpdateHooks()
	{
		Debug.LogWarning($"Downloading hooks from '{HookUrl}'");
		CurrentHookSource = new Il2CppSystem.Net.WebClient().DownloadString(HookUrl);
		Console.WriteLine($"Done.");
	}

	public static void Reload()
	{
		try
		{
			UpdateHooks();

			Debug.Log($"Loading hooks");
			CurrentManifest = JsonConvert.DeserializeObject<Manifest>(CurrentHookSource);
			CurrentManifest.Setup();
			CurrentManifest.Validate();
		}
		catch (Exception ex)
		{
			Debug.LogError($"Failed loading hooks from URL '{HookUrl}' ({ex.Message}\n{ex.StackTrace}");
		}
	}

	public static void Patch()
	{
		var thread = new CarbonHookCompilation()
		{
			Source = CurrentManifest.CreatePatch(),
			FileName = Guid.NewGuid().ToString("N"),
			FilePath = string.Empty,
			Hash = string.Empty
		};
		thread.Start();
	}

	public class Manifest
	{
		public Dictionary<string, List<Hook>> Hooks { get; set; }

		public void Setup()
		{
			foreach (var category in Hooks)
			{
				foreach (var hook in category.Value)
				{
					hook.Setup(category.Key);
				}
			}
		}
		public void Validate()
		{
			var failCount = 0;
			foreach(var category in Hooks)
			{
				foreach(var hook in category.Value)
				{
					if (!hook.Validate())
					{
						failCount++;
					}
				}
			}

			Debug.LogWarning($"Hook manifest report: {failCount} / {Hooks.Sum(x => x.Value.Count)} failed");
		}

		public string CreatePatch(bool validate = true)
		{
			const string doubleSpace = "\n\n";
			var patch = @$"using System;
using Carbon.Client.API;
using Carbon.Client.Base;
using HarmonyLib;

";

			foreach(var category in Hooks)
			{
				foreach(var hook in category.Value)
				{
					if (validate && !hook.Validate()) continue;

					patch += $"{hook.CreatePatch()}{doubleSpace}";
				}
			}

			return patch;
		}

		public class Hook
		{
			internal const BindingFlags _flags =
				BindingFlags.Instance
				| BindingFlags.Static
				| BindingFlags.Public
				| BindingFlags.NonPublic;

			public string HookName { get; set; }
			public string[] HookParameters { get; set; }

			public bool ReturnNonNull { get; set; }
			public string PatchType { get; set; }
			public string PatchMethod { get; set; }
			public string PatchReturnType { get; set; }
			public string[] PatchParameters { get; set; }

			public string[] MetadataDescription { get; set; }
			public string[] MetadataParameters { get; set; }

			[JsonIgnore]
			public bool IsInvalid { get; set; }

			[JsonIgnore]
			public string Category { get; set; }

			[JsonIgnore]
			public bool IsPostfix => string.IsNullOrEmpty(PatchReturnType);

			internal Harmony _patch;

			public void Setup(string category)
			{
				Category = category;	
			}
			public bool Validate()
			{
				var passed = true;

				if (FindType(PatchType) == null)
				{
					Debug.LogWarning($"[{HookName}] Patch type not found: {PatchType}");
					passed = false;
				}

				if (PatchParameters != null)
				{
					foreach (var parameter in PatchParameters)
					{
						if (FindType(parameter) == null)
						{
							Debug.LogWarning($"[{HookName}] Type not found for patch parameter: {parameter}");
							passed = false;
						}
					}
				}

				IsInvalid = !passed;
				return passed;
			}

			public string CreatePatch()
			{
				var type = FindType(PatchType);
				if (type == null)
				{
					Debug.LogError($"[{HookName}] Failed patching: patch type '{PatchType}' is invalid");
					return null;
				}

				var types = PatchParameters.Select(x => FindType(x)).ToArray();
				var method = type.GetMethod(PatchMethod, _flags, null, types, null);
				if (method == null)
				{
					Debug.LogError($"[{HookName}] Failed patching: patch type method '{PatchMethod}' can't be found");
					Dispose();
					return null;
				}

				var parameterList = new List<string>();
				var parameterTypes = method.GetParameters();

				var parameterTypesString = parameterTypes.Select(x => $"{x.ParameterType.FullName} {x.Name}").ToString(", ");
				if(!string.IsNullOrEmpty(parameterTypesString)) parameterList.Add(parameterTypesString);

				if (!method.IsStatic) parameterList.Add($"{type.FullName} __instance");
				if (method.ReturnType != typeof(void)) parameterList.Add($"ref {method.ReturnType.FullName} __result");

				var parameters = parameterList.ToString(", ");
				parameterList.Clear();
				parameterList = null;
				var isVoid = method.ReturnType == typeof(void);

				return @$"
[HarmonyPatch(typeof({type.FullName}), ""{PatchMethod}"", new Type[] {{ {types.Select(x => $"typeof({x.FullName})").ToString(", ")} }})]
[Hook(""{HookName}"")]
[HookCategory(""{Category}"")]
public class {Category}_{HookName}_{Guid.NewGuid():N}
{{
	public static {(method.ReturnType == typeof(void) && !ReturnNonNull ? "void" : "bool")} {(IsPostfix && !ReturnNonNull ? "Postfix" : "Prefix")}({parameters})
	{{
		{(string.IsNullOrEmpty(PatchReturnType) && !ReturnNonNull ? @$"HookCaller.CallHook(""{HookName}"", {HookParameters.ToString(", ")});" :
		$@"
		if (HookCaller.CallHook(""{HookName}"", {HookParameters.ToString(", ")}) {(isVoid ? "== null" : $"is {method.ReturnType.FullName} value" )})
		{{{(isVoid ? "" :
"\n			__result = value;")}
			return true;
		}}

		return false;")}
	}}
}}";


				void Dispose()
				{
					Array.Clear(types, 0, types.Length);
				}
			}
		}
	}
}
