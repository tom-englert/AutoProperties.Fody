using System;

namespace AutoProperties
{
    /// <summary>
    /// Apply this attribute to a method that should intercept all auto-property getters of the class that contains the method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class GetInterceptorAttribute : Attribute
    {
    }

    /// <summary>
    /// Apply this attribute to a method that should intercept all auto-property setters of the class that contains the method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SetInterceptorAttribute : Attribute
    {
    }

    /// <summary>
    /// Apply this attribute to all auto-properties that should not be intercepted.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class InterceptIgnoreAttribute : Attribute
    {
    }
}
