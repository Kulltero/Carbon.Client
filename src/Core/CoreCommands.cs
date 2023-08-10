using System.Linq;
using Carbon.Client.API;
using Carbon.Extensions;

namespace Carbon.Client.Core;

public partial class CorePlugin : CarbonClientPlugin
{
	[Command("carbon")]
	public string CarbonInfo(string[] args)
	{
		return "You're running Carbon for client.";
	}

	[Command("c.find")]
	public string Find(string[] args)
	{
		return CommandLoader.Commands.Where(x => x.Name.Contains(args.ToString(" "))).Select(x => $" {x.Name} (by {x.Plugin.Info?.Name})").ToString("\n");
	}

	[Command("c.reloadhooks")]
	public void ReloadHooks(string[] args)
	{
		HookLoader.Reload();
		HookLoader.Patch();
	}
}
