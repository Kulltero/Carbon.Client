using Network;
using UnityEngine;

/*
 *
 * Copyright (c) 2023 Carbon Community 
 * All rights reserved.
 *
 */

namespace Carbon.Client;

public class CarbonCommunityEntity : BaseNetworkable
{
	public const string PrefabName = "assets/testentity.prefab";
	public static uint PrefabId;

	public static void Init()
	{
		PrefabId = StringPool.Add(PrefabName);
	}

	public static CarbonCommunityEntity Singleton { get; internal set; }

	public void Awake()
	{
		Singleton = this; 
	}

	public override bool OnRpcMessage(BasePlayer player, uint rpc, Message msg)
	{
		var pool = StringPool.Get(rpc);

		switch (pool)
		{
			case "rpc_call1":
				Debug.Log($"Rpc called");
				break;
		}

		return true;
	}
}
