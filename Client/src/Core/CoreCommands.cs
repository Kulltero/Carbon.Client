using System.Linq;
using Carbon.Client.API;
using Carbon.Client.Base;
using Carbon.Extensions;

namespace Carbon.Client.Core;

public partial class CorePlugin : CarbonClientPlugin
{
	[Command("carbon")]
	public string CarbonInfo(Arg arg)
	{
		return "You're running Carbon for client.";
	}

	[Command("c.find")]
	public string Find(Arg arg)
	{
		return CommandLoader.Commands.Where(x => x.Name.Contains(arg.GetSentence())).Select(x => $" {x.Name}{(x.Plugin.Info == null ? string.Empty : $" ({x.Plugin.Info.Name})")}").ToString("\n");
	}

	[Command("c.reloadhooks")]
	public void ReloadHooks(Arg arg)
	{
		HookLoader.Reload();
		HookLoader.Patch();
	}

	[Command("c.hookup")]
	private string Hookup(Arg args)
	{
		var result = string.Empty;
		var filter = args.GetString(0);

		foreach (var category in HookLoader.CurrentManifest.Hooks)
		{
			foreach (var hook in category.Value)
			{
				if (hook.HookName.Contains(filter))
				{
					result += $"[{category.Key}] {hook.HookName} ({hook.MetadataParameters.ToString(", ")})\n{hook.MetadataDescription.Select(x => $" - {x}").ToString("\n")}\n - Patches {hook.PatchType}.{hook.PatchMethod}\n";
				}
			}
		}
		return result;
	}
}
