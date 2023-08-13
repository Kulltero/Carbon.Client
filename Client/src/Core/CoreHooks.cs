using System;
using System.Diagnostics;
using System.Linq;
using Carbon.Client.API;
using Network;
using UnityEngine;

namespace Carbon.Client.Core;

public partial class CorePlugin : CarbonClientPlugin
{
	internal readonly char[] _spaceSplit = new char[] { ' ' };

	private object OnCommandSubmit(ConsoleUI ui, string input)
	{
		if (string.IsNullOrEmpty(input)) return null;

		var split = input.Split(_spaceSplit);
		var commandName = split[0].ToLower().Trim();
		var commandArgs = split.Skip(1).ToArray();
		var arg = new Arg
		{
			Player = LocalPlayer.Entity,
			Name = commandName,
			Args = commandArgs
		};

		if (CommandLoader.FindCommand(commandName, out var command))
		{
			arg.Command = command;

			try
			{
				ui.Log($"<color=orange>></color> {input}");

				var result = command.Callback.Invoke(arg);
				if (!string.IsNullOrEmpty(result)) ui.Log(result);
			}
			catch (Exception ex)
			{
				ex = (ex.InnerException ?? ex).Demystify();
                UnityEngine.Debug.LogError($"Failed executing command '{command.Name}' ({ex.Message})\n{ex.StackTrace}");
			}

			ui.inputField.ActivateInputField();
			ui.inputField.Select();
			ui.history.Add(input);

			Dispose();
			ui.inputField.text = string.Empty;
			return true;
		}

		Dispose();

		void Dispose()
		{
			arg.Dispose();
			Array.Clear(split, 0, split.Length);
			split = null;
		}

		return null;
	}

    private object OnRPCMessage(BaseEntity entity, BasePlayer player, uint rpc, Message msg)
    {
		if (RPCCaller.CallRPC(entity, player, rpc, msg))
			return true;

		return null;
    }
}
