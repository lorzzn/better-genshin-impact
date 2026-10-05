using System;
using static Vanara.PInvoke.User32;

namespace BetterGenshinImpact.Core.Config;

public static partial class KeyIdConverter
{
    /// <summary>
    /// 将KeyId转换为VK
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static VK ToVK(this KeyId value)
    {
        return value switch
        {
            // 这两个值在VK中没有，抛异常
            KeyId.None => throw new ArgumentOutOfRangeException(nameof(value), "未指定按键，无法转换为VK。"),
            KeyId.Unknown => throw new ArgumentOutOfRangeException(nameof(value), "未知按键，无法转换为VK。"),
            // 剩下的值相同，直接转
            _ => (VK)value,
        };
    }
    /// <summary>
    /// 将VK转换为KeyId
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static KeyId FromVK(VK value)
    {
        // 尝试通过VK的值获取KeyId的枚举名。若成功，表示对应的VK在KeyId支持的范围内，直接转换；否则返回Unknown
        return string.IsNullOrEmpty(Enum.GetName(typeof(KeyId), value)) ? KeyId.Unknown : (KeyId)value;
    }
}
