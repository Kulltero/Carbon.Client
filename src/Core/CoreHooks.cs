using System;
using System.Linq;
using Carbon.Client.API;
using UnityEngine;

namespace Carbon.Client.Core;

public partial class CorePlugin : CarbonClientPlugin
{
	internal readonly char[] _spaceSplit = new char[] { '\n' };

	private object OnCommandSubmit(ConsoleUI ui, string input)
	{
		if (string.IsNullOrEmpty(input)) return null;

		var split = input.Split(_spaceSplit);
		var commandName = split[0].ToLower().Trim();
		var commandArgs = split.Skip(1).ToArray();

		if (CommandLoader.FindCommand(commandName, out var command))
		{
			try
			{
				var result = command.Callback.Invoke(commandArgs);
				if (!string.IsNullOrEmpty(result)) ui.Log(result);
				ui.inputField.ActivateInputField();
				ui.inputField.Select();
			}
			catch (Exception ex)
			{
				Debug.LogError($"Failed executing command '{command.Name}' ({ex.Message})\n{ex.StackTrace}");
			}
			Dispose();
			ui.inputField.text = string.Empty;
			return true;
		}

		Dispose();

		void Dispose()
		{
			Array.Clear(split, 0, split.Length);
			Array.Clear(commandArgs, 0, commandArgs.Length);
			split = null;
			commandArgs = null;
		}

		return null;
	}
}
