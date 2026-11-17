using System.Reflection;

namespace Eplicta.Ipx;

/// <summary>Converts the enumerations of this library to and from the names used in an IPX document.</summary>
public static class IpxWire
{
    public static string ToWire<T>(T value) where T : struct, Enum => Names<T>.ToText[value];

    public static bool TryParse<T>(string text, out T value) where T : struct, Enum
    {
        value = default;
        return text is not null && Names<T>.FromText.TryGetValue(text, out value);
    }

    private static class Names<T> where T : struct, Enum
    {
        public static readonly Dictionary<T, string> ToText = new();
        public static readonly Dictionary<string, T> FromText = new(StringComparer.Ordinal);

        static Names()
        {
            foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var attribute = field.GetCustomAttribute<IpxWireAttribute>() ?? throw new InvalidOperationException($"{typeof(T).Name}.{field.Name} has no wire name.");
                var value = (T)field.GetValue(null);
                ToText[value] = attribute.Name;
                FromText[attribute.Name] = value;
            }
        }
    }
}
