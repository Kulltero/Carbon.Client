using Il2CppInterop.Runtime.InteropTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Carbon.Utility
{
    public static class NonGenericIl2CppConverter
    {
        private static Dictionary<Type, Func<IntPtr, object>> _converters = new();

        private static readonly MethodInfo _getUninitializedObject = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetUninitializedObject))!;
        private static readonly MethodInfo _getTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!;
        private static readonly MethodInfo _createGCHandle = typeof(Il2CppObjectBase).GetMethod("CreateGCHandle")!;
        private static readonly FieldInfo _isWrapped = typeof(Il2CppObjectBase).GetField("isWrapped")!;

        // based on https://github.com/BepInEx/Il2CppInterop/blob/master/Il2CppInterop.Runtime/InteropTypes/Il2CppObjectBase.cs
        // but modified for a non-generic usecase
        public static object Convert(Type type, IntPtr pointer)
        {
            if (!_converters.ContainsKey(type))
            {
                var dynamicMethod = new DynamicMethod($"Initializer<{type.AssemblyQualifiedName}>", type, new[] { typeof(IntPtr) });
                dynamicMethod.DefineParameter(0, ParameterAttributes.None, "pointer");

                var il = dynamicMethod.GetILGenerator();

                if (type.GetConstructor(new[] { typeof(IntPtr) }) is { } pointerConstructor)
                {
                    // Base case: Il2Cpp constructor => call it directly
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Newobj, pointerConstructor);
                }
                else
                {
                    // Special case: We have a parameterless constructor
                    // However, it could be be user-made or implicit
                    // In that case we set the GCHandle and then call the ctor and let GC destroy any objects created by DerivedConstructorPointer

                    // var obj = (T)FormatterServices.GetUninitializedObject(type);
                    il.Emit(OpCodes.Ldtoken, type);
                    il.Emit(OpCodes.Call, _getTypeFromHandle);
                    il.Emit(OpCodes.Call, _getUninitializedObject);
                    il.Emit(OpCodes.Castclass, type);

                    // obj.CreateGCHandle(pointer);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Callvirt, _createGCHandle);

                    // obj.isWrapped = true;
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.Emit(OpCodes.Stsfld, _isWrapped);

                    var parameterlessConstructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
                    if (parameterlessConstructor != null)
                    {
                        // obj..ctor();
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Callvirt, parameterlessConstructor);
                    }
                }
                il.Emit(OpCodes.Ret);

                _converters.Add(type, dynamicMethod.CreateDelegate<Func<IntPtr, object>>());
            }



            return _converters[type](pointer);
        }
    }
}
