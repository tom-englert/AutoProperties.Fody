using System;

namespace AutoProperties
{
    /// <summary>
    /// Extension methods to control backing field access of auto properties.
    /// </summary>
    public static class BackingFieldAccessExtensions
    {
        private const string Message = "This method should have been replaced by AutoProperty.Fody. Make sure AutoProperty.Fody is properly called during your build and the extension method is placed at an auto-property that is a member of the class.";

        /// <summary>
        /// Sets the backing field of the auto-property.
        /// </summary>
        /// <typeparam name="T">The type of the auto property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="value">The value to set.</param>
        /// <remarks>
        /// After the assembly has being weaved, this method call is replaced by code that assigns the value directly to the backing field of the auto-property.
        /// </remarks>
        public static void SetBackingField<T>(this T property, T value)
        {
            throw new NotSupportedException(Message);
        }

        /// <summary>
        /// Sets the value of the auto-property.
        /// </summary>
        /// <typeparam name="T">The type of the auto property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="value">The value to set.</param>
        /// <remarks>
        /// This extension method only has an effect in combination with the <see cref="BypassAutoPropertySettersInConstructorsAttribute"/>, to turn off bypassing the setter inside the constructor when bypassing is on. 
        /// Using this extension elsewhere is just the same as writing "Property = value"
        /// </remarks>
        public static void SetProperty<T>(this T property, T value)
        {
            throw new NotSupportedException(Message);
        }
    }
}
