using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Daihenka.AssetPipeline.ReflectionMagic
{
    public class PrivateReflectionDynamicObjectStatic : PrivateReflectionDynamicObjectBase
    {
        static readonly ConcurrentDictionary<Type, IDictionary<string, IDynamicMember>> s_PropertiesOnType = new ConcurrentDictionary<Type, IDictionary<string, IDynamicMember>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivateReflectionDynamicObjectStatic"/> class, wrapping the specified type.
        /// </summary>
        /// <param name="type">The type to wrap.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is <c>null</c>.</exception>
        public PrivateReflectionDynamicObjectStatic(Type type)
        {
            TargetType = type ?? throw new ArgumentNullException(nameof(type));
        }

        protected override IDictionary<Type, IDictionary<string, IDynamicMember>> PropertiesOnType => s_PropertiesOnType;

        // For static calls, we have the type and the instance is always null
        protected override Type TargetType { get; }

        protected override object Instance => null;

        /// <summary>
        /// The type that the <see cref="PrivateReflectionDynamicObjectStatic"/> wraps.
        /// </summary>
        public override object RealObject => TargetType;

        protected override BindingFlags BindingFlags => BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public dynamic New(params object[] args)
        {
            if (args is null)
                throw new ArgumentNullException(nameof(args));

            Debug.Assert(TargetType != null);

#if NETSTANDARD1_5
            var constructors = TargetType.GetTypeInfo().GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            object result = null;
            for (int i = 0; i < constructors.Length; ++i)
            {
                var constructor = constructors[i];
                var parameters = constructor.GetParameters();

                if (parameters.Length == args.Length)
                {
                    bool found = true;
                    for (int j = 0; j < args.Length; ++j)
                    {
                        if (parameters[j].ParameterType != args[j].GetType())
                        {
                            found = false;
                            break;
                        }
                    }

                    if (found)
                    {
                        result = constructor.Invoke(args);
                        break;
                    }
                }
            }

            if (result is null)
                throw new MissingMethodException($"Constructor that accepts parameters: '{string.Join(", ", args.Select(x => x.GetType().ToString()))}' not found.");

            return result.AsDynamic();
#else
            return Activator.CreateInstance(TargetType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, args, null).AsDynamic();
#endif
        }
    }
}