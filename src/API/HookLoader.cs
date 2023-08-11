using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Carbon.Extensions;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Newtonsoft.Json;
using UnityEngine;

namespace Carbon.Client.API;

public class HookLoader
{
	public static Harmony CurrentPatch { get; internal set; }

	public static Manifest Current { get; internal set; }

	public static Type FindType(string type)
	{
		return AccessTools.TypeByName(type);
	}

	public static string GetHookFile()
	{
		return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "BepInEx", "plugins", "carbonc.json"));
	}

	public static void Reload()
	{
		var path = GetHookFile();
		try
		{
			Debug.Log($"Loading hooks from file '{path}'");

			Current = JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(path));
			Current.Setup();
			Current.Validate();
		}
		catch (Exception ex)
		{
			Debug.LogError($"Failed loading hooks from file '{path}' ({ex.Message}\n{ex.StackTrace}");
		}
	}

	public static void Patch()
	{
		var thread = new CarbonHookCompilation()
		{
			Source = Current.CreatePatch(),
			FileName = Guid.NewGuid().ToString("N"),
			FilePath = string.Empty,
			Hash = string.Empty
		};
		thread.Start();
	}

	public static void Save()
	{
		var path = GetHookFile();
		File.WriteAllText(path, JsonConvert.SerializeObject(Current, Formatting.Indented));
	}

	public class Manifest
	{
		public Dictionary<string, Hook[]> Hooks { get; set; }

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

			Debug.LogWarning($"Hook manifest report: {failCount} / {Hooks.Sum(x => x.Value.Length)} failed");
		}

		public string CreatePatch()
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
					if (!hook.Validate()) continue;

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
				if (!Validate()) return null;

				var type = FindType(PatchType);
				if (type == null)
				{
					Debug.LogError($"[{HookName}] Failed patching: patch type '{PatchType}' is invalid");
					return null;
				}

				var types = PatchParameters.Select(x => FindType(x)).ToArray();
				var method = type.GetMethod(PatchMethod, _flags, types);
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
