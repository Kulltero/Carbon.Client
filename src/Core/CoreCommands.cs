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
}
