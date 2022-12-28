using System;
using System.Reflection;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline.Injector
{
    /// <summary>
    /// Use to pass around information between build tasks.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class InjectContextAttribute : Attribute
    {
        //public string Identifier { get; set; }
        /// <summary>
        /// Stores the how the attribute is used among build tasks.
        /// </summary>
        public ContextUsage Usage { get; set; }
        /// <summary>
        /// Stores whether using the context attribute is optional.
        /// </summary>
        public bool Optional { get; set; }

        /// <summary>
        /// Creates a new context attribute that stores information that can be passed between build tasks.
        /// </summary>
        /// <param name="usage">The usage behavior for the attribute. By default it is set to <see cref="ContextUsage.InOut"/>.</param>
        /// <param name="optional">Set to true if using the attribute is optional. Set to false otherwise.</param>
        public InjectContextAttribute(ContextUsage usage = ContextUsage.InOut, bool optional = false)
        {
            this.Usage = usage;
            Optional = optional;
        }
    }

    /// <summary>
    /// Options for how the attribute is used among build tasks. It can be either injected to and or extracted from a build task.
    /// </summary>
    public enum ContextUsage
    {
        /// <summary>
        /// Use to indicate that the attribute can be injected to and extracted from a build task.
        /// </summary>
        InOut,
        /// <summary>
        /// Use to indicate that the attribute can only be injected to a build task.
        /// </summary>
        In,
        /// <summary>
        /// Use to indicate that the attribute can only be extracted from a build task.
        /// </summary>
        Out
    }

    class ContextInjector
    {
        public static void Inject(IBuildContext context, object obj)
        {
            FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (FieldInfo field in fields)
            {
                object[] attrs = field.GetCustomAttributes(typeof(InjectContextAttribute), true);
                if (attrs.Length == 0)
                    continue;

                InjectContextAttribute attr = attrs[0] as InjectContextAttribute;
                if (attr == null || attr.Usage == ContextUsage.Out)
                    continue;

                object injectionObject;
                if (field.FieldType == typeof(IBuildContext))
                    injectionObject = context;
                else if (!attr.Optional)
                    injectionObject = context.GetContextObject(field.FieldType);
                else
                {
                    IContextObject contextObject;
                    context.TryGetContextObject(field.FieldType, out contextObject);
                    injectionObject = contextObject;
                }

                field.SetValue(obj, injectionObject);
            }
        }

        public static void Extract(IBuildContext context, object obj)
        {
            FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (FieldInfo field in fields)
            {
                object[] attrs = field.GetCustomAttributes(typeof(InjectContextAttribute), true);
                if (attrs.Length == 0)
                    continue;

                InjectContextAttribute attr = attrs[0] as InjectContextAttribute;
                if (attr == null || attr.Usage == ContextUsage.In)
                    continue;

                if (field.FieldType == typeof(IBuildContext))
                    throw new InvalidOperationException("IBuildContext can only be used with the ContextUsage.In option.");

                IContextObject contextObject = field.GetValue(obj) as IContextObject;
                if (!attr.Optional)
                    context.SetContextObject(field.FieldType, contextObject);
                else if (contextObject != null)
                    context.SetContextObject(field.FieldType, contextObject);
            }
        }
    }
}
