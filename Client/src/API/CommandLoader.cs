using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Carbon.Client.Base;
using UnityEngine;

namespace Carbon.Client.API;

public class CommandLoader
{
	public static List<BaseCommand> Commands = new();

	internal const BindingFlags _flag = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static;

	public static bool FindCommand(string name, out BaseCommand command)
	{
		return (command = Commands.FirstOrDefault(x => x.Name == name)) != null;
	}
	public static void RegisterType(Type type, CarbonClientPlugin plugin = null)
	{
		foreach (var method in type.GetMethods(_flag))
		{
			var attribute = method.GetCustomAttribute<CommandAttribute>();

			if (attribute == null)
			{
				continue;
			}

			var name = (string.IsNullOrEmpty(attribute.Name) ? method.Name : attribute.Name).ToLower();

			if (Commands.Any(x => x.Name == name))
			{
				continue;
			}

			Commands.Add(new BaseCommand
			{
				Name = name,
				Plugin = plugin,
				Callback = args =>
				{
					var resultArgs = new object[1];
					resultArgs[0] = args;

					var result = method.Invoke(method.IsStatic ? null : plugin, resultArgs)?.ToString();

					Array.Clear(resultArgs, 0, resultArgs.Length);
					resultArgs = null;
					return result;
				}
			});
			Debug.Log($"Installed command '{name}'");
		}
	}
	public static void UnregisterType(CarbonClientPlugin plugin)
	{
		Commands.RemoveAll(x => x.Plugin == plugin);
	}
}
