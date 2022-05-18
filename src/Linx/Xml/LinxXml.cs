using System;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Linx.Xml;

/// <summary>
/// Extensions to facilitate XML reading.
/// </summary>
public static class LinxXml
{
    private static readonly Regex _rgxTimezone = new(@"(([+-][0-9][0-9]:[0-9][0-9])|Z)$", RegexOptions.Compiled);

    /// <summary>
    /// Convert a xml string to a <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <remarks>Makes sure the timezone is explicit (as opposed to <see cref="System.Xml.XmlConvert"/>, which assumes the local timezone).</remarks>
    public static DateTimeOffset ToDateTimeOffset(string s)
    {
        var result = XmlConvert.ToDateTimeOffset(s);
        if (!_rgxTimezone.IsMatch(s)) throw new ArgumentException("Timezone must be specified explicitely.");
        return result;
    }

    #region single child element access

    /// <summary>
    /// Gets the single child element with the specified name.
    /// </summary>
    /// <exception cref="Exception">There is not exactly one such element.</exception>
    public static XElement Single(this XElement element, XName name) => element.SingleOrDefault(name) ?? throw new Exception($"Missing element \'{name}\' on element \'{element.Name}\'.");

    /// <summary>
    /// Gets the single child element with the specified name, or null if not present.
    /// </summary>
    /// <exception cref="Exception">There are multiple such elements.</exception>
    /// <remarks>Preferable over <see cref="XContainer.Element(XName)"/> since it makes sure there is only one.</remarks>
    public static XElement? SingleOrDefault(this XElement element, XName name)
    {
        // ReSharper disable once GenericEnumeratorNotDisposed
        using var e = element.Elements(name).GetEnumerator();
        if (!e.MoveNext()) return null;
        var single = e.Current;
        if (e.MoveNext()) throw new Exception($"Multiple elements '{name}' on element '{element.Name}'.");
        return single;
    }

    /// <summary>
    /// Gets the root of the specified <paramref name="document"/>, if it has the specified <paramref name="name"/>.
    /// </summary>
    /// <exception cref="Exception">The document root is null or has not the specified name.</exception>
    public static XElement Root(this XDocument document, XName name)
    {
        var root = document.Root;
        if (root == null) throw new Exception("Document has no root.");
        if (root.Name != name) throw new Exception($"Document root is {root.Name}. Expected {name}.");
        return root;
    }

    #endregion

    #region reading from attributes

    /// <summary>
    /// Get the value from an attribute.
    /// </summary>
    /// <returns>The attribute value.</returns>
    /// <exception cref="Exception">The specified attribute does not exist.</exception>
    public static string FromAttribute(this XElement element, XName attributeName)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        if (attributeName == null) throw new ArgumentNullException(nameof(attributeName));

        var attribute = element.Attribute(attributeName);
        if (attribute == null) throw new Exception($"Missing attribute '{attributeName}' on element '{element.Name}'.");
        return attribute.Value;
    }

    /// <summary>
    /// Get the value from an attribute.
    /// </summary>
    /// <returns>The converted attribute value.</returns>
    /// <exception cref="Exception">The specified attribute does not exist.</exception>
    public static T FromAttribute<T>(this XElement element, XName attributeName, Func<string, T> converter) => converter(element.FromAttribute(attributeName));

    /// <summary>
    /// Get the value from an attribute.
    /// </summary>
    /// <returns>The attribute value or null, if the attribute is absent.</returns>
    public static string? FromAttributeOrDefault(this XElement element, XName attributeName)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        if (attributeName == null) throw new ArgumentNullException(nameof(attributeName));
        return element.Attribute(attributeName)?.Value;
    }

    /// <summary>
    /// Get the value from an attribute.
    /// </summary>
    /// <returns>The converted attribute value or the default value of <typeparamref name="T"/>, if the attribute is absent.</returns>
    public static T? FromAttributeOrDefault<T>(this XElement element, XName attributeName, Func<string, T> converter)
    {
        var attr = element.Attribute(attributeName);
        return attr != null ? converter(attr.Value) : default;
    }

    /// <summary>
    /// Get the value from an attribute.
    /// </summary>
    /// <returns>The converted attribute value or null, if the attribute is absent.</returns>
    public static T? FromAttributeOrNull<T>(this XElement element, XName name, Func<string, T> converter)
        where T : struct
    {
        var attr = element.Attribute(name);
        return attr != null ? converter(attr.Value) : default(T?);
    }

    #endregion

    #region reading from elements

    /// <summary>
    /// Get a value from a child element.
    /// </summary>
    /// <returns>The converted element value.</returns>
    /// <exception cref="Exception">There is not exactly one such element.</exception>
    public static T FromElement<T>(this XElement element, XName name, Func<XElement, T> converter) => converter(element.Single(name));

    /// <summary>
    /// Get a value from a child element.
    /// </summary>
    /// <returns>The converted element value or the default of <typeparamref name="T"/>, if the element is absent.</returns>
    /// <exception cref="Exception">There are multiple such elements.</exception>
    public static T? FromElementOrDefault<T>(this XElement element, XName name, Func<XElement, T> converter)
    {
        var single = element.SingleOrDefault(name);
        return single != null ? converter(single) : default;
    }

    /// <summary>
    /// Get a value from a child element.
    /// </summary>
    /// <typeparam name="T">Any value type.</typeparam>
    /// <returns>The converted element value or null, if the element is absent.</returns>
    /// <exception cref="Exception">There are multiple such elements.</exception>
    public static T? FromElementOrNull<T>(this XElement element, XName name, Func<XElement, T> converter)
        where T : struct
    {
        var single = element.SingleOrDefault(name);
        return single != null ? converter(single) : default(T?);
    }

    #endregion
}
