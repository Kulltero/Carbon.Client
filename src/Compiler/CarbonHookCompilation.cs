using System;
using System.Reflection;
using System.Threading.Tasks;
using Carbon.Client.API;
using Carbon.Client.Base;
using Carbon.Extensions;
using HarmonyLib;
using UnityEngine;

namespace Carbon.Client;

public class CarbonHookCompilation : CompileThread
{
	internal const BindingFlags _flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic;

	public override void OnFinished()
	{
		const string _prefix = "Prefix";
		const string _postfix = "Postfix";

		HookLoader.CurrentPatch?.UnpatchSelf();

		HookLoader.CurrentPatch = new HarmonyLib.Harmony($"com.carbon.patch.{Guid.NewGuid():N}");

		if (Assembly != null)
		{
			foreach(var type in Assembly.GetTypes())
			{
				var patch = type.GetCustomAttribute<HarmonyPatch>();
				var hook = type.GetCustomAttribute<HookAttribute>();

				if (patch == null || hook == null)
				{
					continue;
				}

				try
				{
					var prefixMethod = type.GetMethod(_prefix, _flags);
					var postfixMethod = type.GetMethod(_postfix, _flags);

					var prefix = (HarmonyMethod)null;
					var postfix = (HarmonyMethod)null;

					if (prefixMethod != null) prefix = new HarmonyMethod(prefixMethod);
					if (postfixMethod != null) postfix = new HarmonyMethod(postfixMethod);

					HookLoader.CurrentPatch.Patch(patch.info.declaringType.GetMethod(patch.info.methodName, _flags, patch.info.argumentTypes), prefix, postfix, null);
					Debug.Log($"Patched hook '{hook.Name}' ({patch.info.declaringType.FullName}.{patch.info.methodName})");
				}
				catch (Exception ex)
				{
					Debug.LogWarning($"Failed patching hook '{hook.Name}': {type.Name} ({ex.Message})\n{ex.StackTrace}");
				}
			}

			BaseHook.Rebuild(Assembly);
		}
	}
}
