using System.Collections;
using System.ComponentModel;
using System.Reflection;
using Avalux.OpenAi.Client.Models;

namespace Avalux.OpenAi.Client.Internals;

internal static class TypeParser
{
    public static TypeSchema ToSchema(this Type type)
    {
        return type.ToSchema(null);
    }

    private static TypeSchema ToSchema(this Type type, string? description)
    {
        if (type.IsEnum)
            return new TypeSchema
            {
                Type = "string",
                EnumOptions = type.GetEnumNames(),
                Description = description,
            };
        if (type.IsPrimitive)
        {
            if (type.IsAssignableTo(typeof(string)))
                return new TypeSchema
                {
                    Type = "string",
                    Description = description,
                };
            if (type.IsAssignableTo(typeof(int)))
                return new TypeSchema
                {
                    Type = "integer",
                    Description = description,
                };
            if (type.IsAssignableTo(typeof(float)))
                return new TypeSchema
                {
                    Type = "float",
                    Description = description,
                };
            if (type.IsAssignableTo(typeof(bool)))
                return new TypeSchema
                {
                    Type = "boolean",
                    Description = description,
                };
        }

        if (type.IsAssignableTo(typeof(IEnumerable)))
        {
            return new TypeSchema
            {
                Type = "array",
                ItemsType = type.GenericTypeArguments.First().ToSchema(),
                Description = description,
            };
        }

        if (type.IsClass)
        {
            return new TypeSchema
            {
                Type = "object",
                Properties = type.GetProperties()
                    .ToDictionary(e => e.Name, e => e.ToSchema()),
                Description = description,
            };
        }

        throw new InvalidOperationException("This type is not allowed");
    }

    private static TypeSchema ToSchema(this PropertyInfo property)
    {
        var descriptionAttribute = property.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute;
        return property.PropertyType.ToSchema(descriptionAttribute?.Description);
    }
}