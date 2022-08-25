using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace GetSetGenerator
{
    public class ExtensionCompiler
    {
        private const string AssemblyName = "GetSet.Plugh";

        readonly ModuleBuilder _moduleBuilder;

        private readonly Dictionary<(Type, Type, string), object> _cache = new Dictionary<(Type, Type, string), object>();

        public ExtensionCompiler()
        {
            var asmName = new AssemblyName {Name = AssemblyName};
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.RunAndCollect);
            _moduleBuilder = assemblyBuilder.DefineDynamicModule(AssemblyName);
        }

        public IGetSet<TInstance, TField> Create<TInstance, TField>(string fieldName)
        {
            var key = (typeof(TInstance), typeof(TField), fieldName);
            if (!_cache.TryGetValue(key, out object getSetInstance))
                getSetInstance = _cache[key] = _CreateGetSet<TInstance, TField>(fieldName);
            return (IGetSet<TInstance,TField>)getSetInstance;
        }

        private IGetSet<TInstance, TField> _CreateGetSet<TInstance,TField>(string fieldName)
        {
            var fi = typeof(TInstance).GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (fi == null)
                throw new Exception($"Could not find field {fieldName}");
            
            fi = fi.DeclaringType!.GetField(fi.Name, BindingFlags.Public | BindingFlags.Instance);
            if (fi == null)
                throw new Exception($"Could not find field {fieldName}");

            var className = $"GetSet_{fi.DeclaringType!.FullName}_{fi.Name}";
            
            var typeBuilder = _moduleBuilder.DefineType(className, TypeAttributes.Public);
            var interfaceType = typeof(IGetSet<,>).MakeGenericType(fi.DeclaringType, fi.FieldType);
            typeBuilder.AddInterfaceImplementation(interfaceType);

            CreateSetMethod(fi.DeclaringType, fi, typeBuilder, interfaceType);
            CreateGetMethod(fi.DeclaringType, fi, typeBuilder, interfaceType);
            
            return (IGetSet<TInstance,TField>)Activator.CreateInstance(typeBuilder.CreateType());
        }
        
        void CreateSetMethod(Type declaringType, FieldInfo fi, TypeBuilder typeBuilder, Type interfaceType)
        {
            var mb = typeBuilder.DefineMethod("Set", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard, 
                declaringType, new[] { declaringType, fi.FieldType });
            var il = mb.GetILGenerator();
            il.Emit(OpCodes.Ldarga_S, 1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Stfld, fi);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(mb, interfaceType.GetMethod("Set"));
        }

        void CreateGetMethod(Type declaringType, FieldInfo fi, TypeBuilder typeBuilder, Type interfaceType)
        {
            var mb = typeBuilder.DefineMethod("Get", MethodAttributes.Public | MethodAttributes.Virtual, CallingConventions.Standard, 
                fi.FieldType, new[] { declaringType });
            var il = mb.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldfld, fi);
            il.Emit(OpCodes.Ret);
            typeBuilder.DefineMethodOverride(mb, interfaceType.GetMethod("Get"));
        }
    }
}