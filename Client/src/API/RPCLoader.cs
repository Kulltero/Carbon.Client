using System;
using System.Collections.Generic;
using System.Reflection;
using Carbon.Client.Base;
using Carbon.Utility;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace Carbon.Client.API;

public class RPCLoader
{

    public static Dictionary<uint, List<BaseRPC>> RPCMethods = new();

	internal const BindingFlags _flag = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
	
	internal static Type baseEntityType = typeof(BaseEntity);

    private static object[] staticArgs = new object[2];

    private static object[] instanceArgs = new object[1];

	public static bool FindRPCs(uint rpc, out List<BaseRPC> rpcMethods)
	{
		return RPCMethods.TryGetValue(rpc, out rpcMethods);
	}
	public static void RegisterPlugin(CarbonClientPlugin plugin)
	{
        var type = plugin.GetType();
		RegisterType(type, plugin);
        foreach(var child in type.GetNestedTypes())
        {
            RegisterType(child, plugin);
        }
	}

	private static void RegisterType(Type type, CarbonClientPlugin plugin)
	{
        foreach (var method in type.GetMethods(_flag))
        {
            var attribute = method.GetCustomAttribute<ClientRPCAttribute>();
            if (attribute == null)
            {
                continue;
            }

            bool isPluginType = typeof(CarbonClientPlugin).IsAssignableFrom(type);
            Type entityType = null;
            var name = (string.IsNullOrEmpty(attribute.Name) ? method.Name : attribute.Name);
            var rpc = StringPool.Add(name);


            // attempt to find the entity type:
            // if the method has 2 parameters, assume the first is the entity
            // if the method only has one, assume its the type itself is the entity type
            // otherwise skip as this RPC has an invalid signature
            var parameters = method.GetParameters();
            if(parameters.Length == 0) {
                continue;
            }
            else if (parameters.Length >= 2)
            {
                entityType = parameters[0].ParameterType;
            } 
            else
            {
                if (isPluginType)
                    continue;
                entityType = type;
            }
            if (!baseEntityType.IsAssignableFrom(entityType))
                continue;

            if (!ClassInjector.IsTypeRegisteredInIl2Cpp(entityType))
                ClassInjector.RegisterTypeInIl2Cpp(entityType);
            var il2cppEntityType = Il2CppType.From(entityType);
            Add(new BaseRPC
            {
                Name = name,
                ID = rpc,
                Plugin = plugin,
                EntityType = il2cppEntityType,
                InstanceRPC = isPluginType,
                Callback = (entity, message) =>
                {
                    var ob = (entity as Il2CppSystem.Object);
                    
                    // the Il2Cpp way of checking and casting one type to another at runtime
                    if (!il2cppEntityType.IsAssignableFrom(ob.GetIl2CppType()))
                        return false;
                    entity = NonGenericIl2CppConverter.Convert(entityType, ob.Pointer);

                    object instance = null;
                    object result = null;
                    object[] args = null;

                    if (method.IsStatic)
                    {
                        args = staticArgs;
                        args[0] = entity;
                        args[1] = message;
                    }
                    else if (!isPluginType)
                    {
                        args = instanceArgs;
                        args[0] = message;
                        instance = entity;
                    }
                    else
                    {
                        args = staticArgs;
                        args[0] = entity;
                        args[1] = message;
                        instance = plugin;
                    }
                    result = method.Invoke(instance, args);
                    Array.Clear(args, 0, args.Length);
                    return (result is bool bResult ? bResult : false);
                }
            });

            Debug.Log($"Installed RPC '{name}' [{type.Name}]");
        }
    }
    
    private static void Add(BaseRPC method)
    {
        if (!RPCMethods.TryGetValue(method.ID, out List<BaseRPC> list))
            RPCMethods.Add(method.ID, list = new List<BaseRPC>());

        list.Add(method);
    }

	public static void UnregisterPlugin(CarbonClientPlugin plugin)
	{
        foreach (var kvp in RPCMethods)
        {
            for(int i = kvp.Value.Count - 1; i >= 0; i--)
            {
                if (kvp.Value[i].Plugin == plugin)
                    kvp.Value.RemoveAt(i);
            }
        }
	}
}
