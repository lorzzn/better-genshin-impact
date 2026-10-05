using System.Security.Cryptography;
using System.Text;

namespace BetterGenshinImpact.Runtime;

/// <summary>Writable storage for the official SIFT builder; source maps stay read-only.</summary>
public static class MapFeatureCache
{
    private static readonly object sync = new();

    public static void Ensure(string imagePath, Action<string, string, string> saveFeatures)
    {
        var (keys, descriptors) = SourcePaths(imagePath);
        if (File.Exists(keys) && File.Exists(descriptors)) return;
        var directory = CacheDirectory(imagePath);
        lock (sync)
        {
            if (Complete(directory, keys, descriptors)) return;
            Directory.CreateDirectory(directory);
            File.Delete(Path.Combine(directory, ".complete"));
            var temporary = Path.Combine(directory, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporary);
            try
            {
                var tempKeys = Path.Combine(temporary, Path.GetFileName(keys));
                var tempDescriptors = Path.Combine(temporary, Path.GetFileName(descriptors));
                saveFeatures(imagePath, tempKeys, tempDescriptors);
                File.Move(tempKeys, Path.Combine(directory, Path.GetFileName(keys)), true);
                File.Move(tempDescriptors, Path.Combine(directory, Path.GetFileName(descriptors)), true);
                // A canceled pair is never treated as a complete cache entry.
                File.WriteAllText(Path.Combine(directory, ".complete"), "1");
            }
            finally { Directory.Delete(temporary, true); }
        }
    }

    public static string[] LayerFiles(string layerDirectory)
    {
        var source = Directory.GetFiles(layerDirectory);
        var files = source.ToDictionary(Path.GetFileName, p => p, StringComparer.Ordinal);
        foreach (var image in source.Where(p => (p.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
            && !p.EndsWith("_SIFT.mat.png", StringComparison.Ordinal)))
        {
            var (keys, descriptors) = SourcePaths(image);
            if (File.Exists(keys) && File.Exists(descriptors)) continue;
            var directory = CacheDirectory(image);
            if (!Complete(directory, keys, descriptors)) continue;
            files[Path.GetFileName(keys)] = Path.Combine(directory, Path.GetFileName(keys));
            files[Path.GetFileName(descriptors)] = Path.Combine(directory, Path.GetFileName(descriptors));
        }
        return files.Values.ToArray();
    }

    private static bool Complete(string directory, string keys, string descriptors) =>
        File.Exists(Path.Combine(directory, ".complete")) &&
        File.Exists(Path.Combine(directory, Path.GetFileName(keys))) &&
        File.Exists(Path.Combine(directory, Path.GetFileName(descriptors)));

    private static (string Keys, string Descriptors) SourcePaths(string image)
    {
        var prefix = Path.Combine(Path.GetDirectoryName(image)!, Path.GetFileNameWithoutExtension(image));
        return (prefix + "_SIFT.kp.bin", prefix + "_SIFT.mat.png");
    }

    private static string CacheDirectory(string image)
    {
        var file = new FileInfo(image);
        var identity = string.Join("\n", typeof(MapFeatureCache).Assembly.GetName().Version, file.FullName, file.Length, file.LastWriteTimeUtc.Ticks);
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)));
        return Path.Combine(RuntimeEnvironment.StateRoot, "Cache", "MapFeatures", key);
    }
}
