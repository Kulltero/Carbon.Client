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
	public override void OnFinished()
	{
		if (Assembly != null)
		{
			BaseHook.Rebuild(Assembly);
		}
	}
}
