using System.Globalization;
using System.Numerics;
using YamlDotNet.RepresentationModel;

namespace UnitySceneReader;

internal class Reader
{
    public static long ReadFileId(YamlNode node)
    {
        var value1 = ((YamlMappingNode)node).Children[new YamlScalarNode("fileID")];
        var val = ((YamlScalarNode)value1).Value;
        long id = long.Parse(val);
        return id;
    }

    public static Guid ReadGuid(YamlNode node)
    {
        var value = ((YamlMappingNode)node).Children[new YamlScalarNode("guid")];
        var val = ((YamlScalarNode)value).Value;
        Guid id = Guid.Parse(val);
        return id;
    }

    public static Quaternion ReadQuaternion(YamlMappingNode quaternionMapping)
    {
        float x = 0, y = 0, z = 0, w = 0;

        // Extract x, y, z, w from the YamlMappingNode
        foreach (var entry in quaternionMapping.Children)
        {
            var key = entry.Key.ToString();
            var value = entry.Value.ToString();

            switch (key)
            {
                case "x":
                    x = float.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case "y":
                    y = float.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case "z":
                    z = float.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case "w":
                    w = float.Parse(value, CultureInfo.InvariantCulture);
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected property '{key}' for Quaternion.");
            }
        }

        return new Quaternion(x, y, z, w);
    }

    public static Vector3 ReadVector3(YamlMappingNode vectorMapping)
    {
        float x = 0, y = 0, z = 0;

        // Extract x, y, z from the YamlMappingNode
        foreach (var entry in vectorMapping.Children)
        {
            var key = entry.Key.ToString();
            var value = entry.Value.ToString();

            switch (key)
            {
                case "x":
                    x = float.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case "y":
                    y = float.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case "z":
                    z = float.Parse(value, CultureInfo.InvariantCulture);
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected property '{key}' for Vector3.");
            }
        }

        return new Vector3(x, y, z);
    }

    public static string ReadString(YamlNode node, string key)
    {
        var map = (YamlMappingNode)node;
        if (map.Children.TryGetValue(new YamlScalarNode(key), out var value))
        {
            var val = ((YamlScalarNode)value).Value;
            return val;
        }

        return default;
    }

    public static long TryReadFileId(YamlNode node, string key)
    {
        var map = (YamlMappingNode)node;
        if (map.Children.TryGetValue(new YamlScalarNode(key), out var value))
        {
            var value1 = ((YamlMappingNode)value).Children[new YamlScalarNode("fileID")];
            var val = ((YamlScalarNode)value1).Value;
            long id = long.Parse(val);
            return id;
        }
        else
        {
            return default;
        }
    }

    public static T Read<T>(YamlNode node, string key) where T : IParsable<T>
    {
        var map = (YamlMappingNode)node;
        if (map.Children.TryGetValue(new YamlScalarNode(key), out var value))
        {
            var val = ((YamlScalarNode)value).Value;
            if (T.TryParse(val, CultureInfo.InvariantCulture, out var id))
            {
                return id;
            }
        }

        return default;
    }
}