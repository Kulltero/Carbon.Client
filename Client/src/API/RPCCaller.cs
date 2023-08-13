using System;
using System.Collections.Generic;
using Carbon.Client.Base;
using Network;
using UnityEngine;

namespace Carbon.Client.API;

public class RPCCaller
{

    public static BaseEntity.RPCMessage reusableRPC = new();

    public static bool CallRPC(BaseEntity entity, BasePlayer player, uint rpc, Message message)
    {

        bool result = false;
        if (!RPCLoader.FindRPCs(rpc, out List<BaseRPC> method))
			return result;

		if(method.Count == 0) 
			return result;

        reusableRPC.player = player;
        reusableRPC.connection = message.connection;
        reusableRPC.read = message.read;
        for (int i = 0; i < method.Count; i++)
		{
			var methodResult = method[i].Callback?.Invoke((BaseEntity)entity, reusableRPC) ?? false;
			if(methodResult == true)
			{
				result = true;
				// Discression for Raul: uncomment this if you only want to call RPC methods until the call is handled
				// break;
			}
        }

		reusableRPC.player = null;
		reusableRPC.connection = null;
		reusableRPC.read = null;

		return result;
	}
}
