using System.Collections;
using System.Globalization;
using System.Reflection;

namespace PDML.Generator.Abstractions;

public static class GeneratorSettingsMapper
{
    public static TSettings Map<TSettings>(ILookup<string, string>? source)
        where TSettings : GeneratorSettings, new()
        => (TSettings)Map(source, typeof(TSettings));

    public static GeneratorSettings Map(ILookup<string, string>? source, Type settingsType)
    {
        if (source is null)
        {
            // Initalize an empty ILookup
            source = Enumerable.Empty<KeyValuePair<string, string>>().ToLookup(x => x.Key, x => x.Value);
        }

        ArgumentNullException.ThrowIfNull(settingsType);

        if (!typeof(GeneratorSettings).IsAssignableFrom(settingsType))
        {
            throw new ArgumentException($"Type must inherit from '{typeof(GeneratorSettings).FullName}'", nameof(settingsType));
        }

        if (settingsType.IsAbstract || settingsType.GetConstructor(Type.EmptyTypes) is null)
        {
            throw new ArgumentException("Type must be concrete and have a public parameterless constructor", nameof(settingsType));
        }

        GeneratorSettings? result = (GeneratorSettings?)Activator.CreateInstance(settingsType);
        if (result is null)
        {
            throw new InvalidOperationException($"Failed to instantiate '{settingsType.FullName}'");
        }

        PropertyInfo[] properties = settingsType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        NullabilityInfoContext nullability = new NullabilityInfoContext();

        foreach (PropertyInfo property in properties)
        {
            if (property.SetMethod is null || !property.SetMethod.IsPublic)
            {
                continue;
            }

            IGrouping<string, string>? group = source.FirstOrDefault(g => string.Equals(g.Key, property.Name, StringComparison.OrdinalIgnoreCase));
            NullabilityInfo nullableInfo = nullability.Create(property);
            Type? nullableType = Nullable.GetUnderlyingType(property.PropertyType);

            bool isNullable = nullableType is not null || (!property.PropertyType.IsValueType && nullableInfo.WriteState == NullabilityState.Nullable);

            if (group is null)
            {
                if (!isNullable)
                {
                    throw new SettingsMappingException($"Required setting '{property.Name}' is missing");
                }

                property.SetValue(result, null);
                continue;
            }

            string[] values = group.ToArray();

            try
            {
                object? mappedValue;

                if (property.PropertyType.IsArray)
                {
                    Type elementType = property.PropertyType.GetElementType()!;

                    Array array = Array.CreateInstance(elementType, values.Length);
                    for (int i = 0; i < values.Length; i++)
                    {
                        array.SetValue(
                            ConvertValue(values[i], elementType),
                            i
                        );
                    }

                    mappedValue = array;
                }
                else if (TryGetCollectionElementType(
                    property.PropertyType,
                    out Type elementType
                ))
                {
                    Type listType = typeof(List<>).MakeGenericType(elementType);

                    IList list = (IList)Activator.CreateInstance(listType)!;

                    foreach (string item in values)
                    {
                        list.Add(ConvertValue(item, elementType));
                    }

                    if (!property.PropertyType.IsAssignableFrom(listType))
                    {
                        throw new InvalidOperationException($"Collection type '{property.PropertyType}' cannot be assigned from List<T>");
                    }

                    mappedValue = list;
                }
                else
                {
                    if (values.Length > 1)
                    {
                        throw new InvalidOperationException($"Settings '{property.Name}' has multiple values but is not a collection.");
                    }

                    mappedValue = ConvertValue(values[0], property.PropertyType);
                }

                property.SetValue(result, mappedValue);
            }
            catch (Exception ex) when (ex is not SettingsMappingException)
            {
                throw new SettingsMappingException($"Failed to map settings '{property.Name}'.", ex);
            }
        }

        return result;
    }

    private static object? ConvertValue(string? value, Type targetType)
    {
        if (value is null)
        {
            if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) is not null)
            {
                return null;
            }

            throw new InvalidOperationException($"Cannot assign null to '{targetType.FullName}'");
        }

        Type effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (effectiveType.IsInstanceOfType(value))
        {
            return value;
        }

        if (effectiveType == typeof(string))
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        if (effectiveType.IsEnum)
        {
            return value is string text
                ? Enum.Parse(effectiveType, text, ignoreCase: true)
                : Enum.ToObject(effectiveType, value);
        }

        return Convert.ChangeType(value, effectiveType, CultureInfo.InvariantCulture);
    }

    private static bool TryGetCollectionElementType(
        Type type,
        out Type elementType
    )
    {
        Type? enumerableType = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        elementType = enumerableType?.GetGenericArguments()[0] ?? typeof(object);

        return enumerableType is not null && type.IsAssignableFrom(typeof(List<>).MakeGenericType(elementType));
    }
}
