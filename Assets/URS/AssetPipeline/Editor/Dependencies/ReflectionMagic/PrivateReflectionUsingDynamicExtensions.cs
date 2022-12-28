using System;
using System.Reflection;

namespace Daihenka.AssetPipeline.ReflectionMagic
{
    public static class PrivateReflectionUsingDynamicExtensions
    {
        /// <summary>
        /// Wraps the specified object in a dynamic object that allows access to private members.
        /// </summary>
        /// <param name="o">The object to wrap</param>
        /// <returns>The wrapped object.</returns>
        /// <remarks>
        /// Does not wrap <c>null</c>, <see cref="string"/>, primitive types, and already wrapped objects.
        /// </remarks>
        /// <seealso cref="PrivateReflectionDynamicObjectInstance"/>
        public static dynamic AsDynamic(this object o)
        {
            // Don't wrap primitive types, which don't have many interesting internal APIs
            if (o is null || o.GetType().GetTypeInfo().IsPrimitive || o is string || o is PrivateReflectionDynamicObjectBase)
                return o;

            return new PrivateReflectionDynamicObjectInstance(o);
        }

        /// <summary>
        /// Wraps the specified type in a dynamic object which allows easy instantion through the <see cref="PrivateReflectionDynamicObjectStatic.New"/> method.
        /// </summary>
        /// <param name="type">The type to wrap.</param>
        /// <returns>The wrapped type.</returns>
        /// <seealso cref="PrivateReflectionDynamicObjectStatic"/>
        public static dynamic AsDynamicType(this Type type)
        {
            return new PrivateReflectionDynamicObjectStatic(type);
        }

        /// <summary>
        /// Gets the type with the specified name from the specified assembly instance, and returns it as a dynamic object. See also <see cref="AsDynamicType"/>.
        /// </summary>
        /// <param name="assembly">The assembly instance to search for the type.</param>
        /// <param name="typeName">The type name.</param>
        /// <returns>The wrapped type.</returns>
        /// <seealso cref="AsDynamicType"/>
        public static dynamic GetDynamicType(this Assembly assembly, string typeName)
        {
            if (assembly is null)
                throw new ArgumentNullException(nameof(assembly));

            return assembly.GetType(typeName).AsDynamicType();
        }

        /// <summary>
        /// Tries to instantiate the type with the specified type name from the specified assembly instance using the specified constructor arguments.
        /// </summary>
        /// <param name="assembly">The assembly instance to search.</param>
        /// <param name="typeName">The full type name.</param>
        /// <param name="args">The arguments to pass to the constructor.</param>
        /// <returns></returns>
        /// <exception cref="MissingMethodException">Thrown when no suitable constructor can be found.</exception>
        public static dynamic CreateDynamicInstance(this Assembly assembly, string typeName, params object[] args)
        {
            if (args is null)
                throw new ArgumentNullException(nameof(args));

            return assembly.GetDynamicType(typeName).New(args);
        }
    }
}