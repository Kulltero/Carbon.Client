using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Carbon.Client.Base;

public class BaseCommand
{
	public CarbonClientPlugin Plugin { get; set; }

	public string Name { get; set; }

	public Func<string[], string> Callback { get; set; }
}
