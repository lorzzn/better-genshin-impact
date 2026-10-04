using System;
using System.IO;
using System.Linq;

namespace BetterGenshinImpact.Core.Script.Utils;

public class ScriptUtils
{
    /// <summary>
    /// Normalize and validate a path.
    /// </summary>
    public static string NormalizePath(string root, string path)
    {
        // 校验空字符串
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("文件路径不能为空");

        // 检查是否含有非法文件名字符
        var invalidChars = Path.GetInvalidFileNameChars();
        string fileName = Path.GetFileName(path);
        if (fileName.Any(c => invalidChars.Contains(c)))
        {
            throw new ArgumentException($"文件路径 '{path}' 包含非法字符");
        }

        // 替换分隔符
        path = path.Replace('\\', '/');

        // 组合并获取绝对路径
        root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        var fullPath = Path.GetFullPath(Path.Combine(root, path));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        // 防止越界访问
        if (!fullPath.Equals(root, comparison) && !fullPath.StartsWith(root + Path.DirectorySeparatorChar, comparison))
        {
            throw new ArgumentException($"文件路径 '{path}' 越界访问!");
        }

        // A lexical child may point outside the package through a symlink or
        // Windows junction. Script packages use ordinary files/directories.
        for (var entry = fullPath; entry != null; entry = Path.GetDirectoryName(entry))
        {
            if ((File.Exists(entry) || Directory.Exists(entry)) &&
                (File.GetAttributes(entry) & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException($"文件路径 '{path}' 包含符号链接或目录联接!");
            if (entry.Equals(root, comparison)) break;
        }

        return fullPath;
    }
}
