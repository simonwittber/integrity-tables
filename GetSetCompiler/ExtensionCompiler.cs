using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GetSetGenerator
{
    public class ExtensionCompiler
    {
        private const string AssemblyName = "GetSet.Plugh";

        private readonly ModuleBuilder _moduleBuilder;

        private readonly Dictionary<Type, object> _cache = new Dictionary<Type, object>();

        public ExtensionCompiler()
        {
            var asmName = new AssemblyName {Name = AssemblyName};
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndCollect);
            _moduleBuilder = assemblyBuilder.DefineDynamicModule(AssemblyName);
        }

        public IFieldIndexer<TInstance> Create<TInstance>()where TInstance:struct
        {
            var key = typeof(TInstance);
            if (!_cache.TryGetValue(key, out object indexerInstance))
                indexerInstance = _cache[key] = _CreateFieldIndexer<TInstance>();
            return (IFieldIndexer<TInstance>)indexerInstance;
        }

        private IFieldIndexer<TInstance> _CreateFieldIndexer<TInstance>()where TInstance:struct
        {
            var fields = typeof(TInstance).GetFields(BindingFlags.Public | BindingFlags.Instance);
            
            
            var className = $"FieldIndex";
            var typeBuilder = _moduleBuilder.DefineType(className, TypeAttributes.Public);
            
            var interfaceType = typeof(IFieldIndexer<>).MakeGenericType(typeof(TInstance));
            typeBuilder.AddInterfaceImplementation(interfaceType);


            CreateGetMethod<TInstance>(typeBuilder, fields, interfaceType);
            CreateTypesMethod<TInstance>(typeBuilder, fields, interfaceType);
            CreateNamesMethod<TInstance>(typeBuilder, fields, interfaceType);

            return (IFieldIndexer<TInstance>)Activator.CreateInstance(typeBuilder.CreateType());

        }

        private static void CreateGetMethod<TInstance>(TypeBuilder typeBuilder, FieldInfo[] fields, Type interfaceType) where TInstance : struct
        {
            var mb = typeBuilder.DefineMethod(
                "Get",
                MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard,
                typeof(object),
                new[] {typeof(TInstance), typeof(int)});

            var il = mb.GetILGenerator();
            var defaultLabel = il.DefineLabel();
            var jumpTable = (from i in fields select il.DefineLabel()).ToArray();
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Switch, jumpTable);
            il.Emit(OpCodes.Br_S, defaultLabel);
            for (var i = 0; i < fields.Length; i++)
            {
                il.MarkLabel(jumpTable[i]);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldfld, fields[i]);
                if (fields[i].FieldType.IsValueType)
                    il.Emit(OpCodes.Box, fields[i].FieldType);
                il.Emit(OpCodes.Ret);
            }

            il.MarkLabel(defaultLabel);
            il.ThrowException(typeof(IndexOutOfRangeException));
            typeBuilder.DefineMethodOverride(mb, interfaceType.GetMethod("Get"));
        }
        
        private static void CreateTypesMethod<TInstance>(TypeBuilder typeBuilder, FieldInfo[] fields, Type interfaceType) where TInstance : struct
        {
            var mb = typeBuilder.DefineMethod(
                "Types",
                MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard, typeof(Type[]), Type.EmptyTypes);

            var il = mb.GetILGenerator();
            il.Emit(OpCodes.Ldc_I4, fields.Length);
            il.Emit(OpCodes.Newarr, typeof(Type));
            il.Emit(OpCodes.Dup);
            for (var i = 0; i < fields.Length; i++)
            {
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldtoken, fields[i].FieldType);
                il.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
                il.Emit(OpCodes.Stelem_Ref);
                if (i < fields.Length - 1)
                    il.Emit(OpCodes.Dup);
            }
            il.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(mb, interfaceType.GetMethod("Types"));
        }
        
        private static void CreateNamesMethod<TInstance>(TypeBuilder typeBuilder, FieldInfo[] fields, Type interfaceType) where TInstance : struct
        {
            var mb = typeBuilder.DefineMethod(
                "Names",
                MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard, typeof(string[]), Type.EmptyTypes);

            var il = mb.GetILGenerator();
            il.Emit(OpCodes.Ldc_I4, fields.Length);
            il.Emit(OpCodes.Newarr, typeof(string));
            il.Emit(OpCodes.Dup);
            for (var i = 0; i < fields.Length; i++)
            {
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldstr, fields[i].Name);
                il.Emit(OpCodes.Stelem_Ref);
                if (i < fields.Length - 1)
                    il.Emit(OpCodes.Dup);
            }
            il.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(mb, interfaceType.GetMethod("Names"));
        }
    }
}