using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Windows.Forms;
using System.Windows.Input;
using Windows.Networking.PushNotifications;
using static Vanara.PInvoke.User32;

namespace BetterGenshinImpact.Core.Config;

public static partial class KeyIdConverter
{

    /// <summary>
    /// 将KeyId转换为字符串（可在后续支持多语言），按键名称的显示尽量与原神UI一致
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ToName(this KeyId value)
    {
        return ToChineseName(value);
    }

    private static string ToChineseName(KeyId value) 
    {
        return value switch
        {
            // 需要单独翻译的按键
            KeyId.None => "<未指定>",
            KeyId.Unknown => "<未知>",
            KeyId.MouseLeftButton => "鼠标左键",
            KeyId.MouseRightButton => "鼠标右键",
            KeyId.MouseMiddleButton => "鼠标中键",
            KeyId.MouseSideButton1 => "鼠标侧键1",
            KeyId.MouseSideButton2 => "鼠标侧键2",
            KeyId.Apps => "菜单键",
            // 无需单独翻译的部分
            _ => EnglishKeyNameToChinese(value),
        };
    }

    private static string EnglishKeyNameToChinese(KeyId value)
    {
        var engName = ToEnglishName(value);
        if (engName.StartsWith("Left ") || engName.StartsWith("Right "))
        {
            return engName.Replace("Left ", "左").Replace("Right ", "右");
        }
        return engName;
    }

    private static string ToEnglishName(KeyId value)
    {
        return value switch
        {
            // 需要转换的部分
            KeyId.None => "<None>",
            KeyId.Unknown => "<Unknown>",
            KeyId.MouseLeftButton => "Mouse LButton",
            KeyId.MouseRightButton => "Mouse RButton",
            KeyId.MouseMiddleButton => "Mouse MButton",
            KeyId.MouseSideButton1 => "Mouse XButton1",
            KeyId.MouseSideButton2 => "Mouse XButton2",
            KeyId.Escape => "Esc",
            KeyId.PageUp => "Page Up",
            KeyId.PageDown => "Page Down",
            KeyId.CapsLock => "Caps Lock",
            KeyId.ScrollLock => "Scroll Lock",
            KeyId.LeftShift => "Left Shift",
            KeyId.RightShift => "Right Shift",
            KeyId.LeftCtrl => "Left Ctrl",
            KeyId.RightCtrl => "Right Ctrl",
            KeyId.LeftAlt => "Left Alt",
            KeyId.RightAlt => "Right Alt",
            KeyId.LeftWin => "Left Win",
            KeyId.RightWin => "Right Win",
            KeyId.Apps => "Menu",
            KeyId.Left => "←",
            KeyId.Up => "↑",
            KeyId.Right => "→",
            KeyId.Down => "↓",
            KeyId.D0 => "0",
            KeyId.D1 => "1",
            KeyId.D2 => "2",
            KeyId.D3 => "3",
            KeyId.D4 => "4",
            KeyId.D5 => "5",
            KeyId.D6 => "6",
            KeyId.D7 => "7",
            KeyId.D8 => "8",
            KeyId.D9 => "9",
            KeyId.Apostrophe => "'",
            KeyId.Comma => ",",
            KeyId.Minus => "-",
            KeyId.Equal => "=",
            KeyId.Period => ".",
            KeyId.Slash => "/",
            KeyId.Backslash => @"\",
            KeyId.Semicolon => ";",
            KeyId.LeftSquareBracket => "[",
            KeyId.RightSquareBracket => "]",
            KeyId.Tilde => "`",
            KeyId.NumLock => "Num Lock",
            KeyId.NumPad0 => "Num 0",
            KeyId.NumPad1 => "Num 1",
            KeyId.NumPad2 => "Num 2",
            KeyId.NumPad3 => "Num 3",
            KeyId.NumPad4 => "Num 4",
            KeyId.NumPad5 => "Num 5",
            KeyId.NumPad6 => "Num 6",
            KeyId.NumPad7 => "Num 7",
            KeyId.NumPad8 => "Num 8",
            KeyId.NumPad9 => "Num 9",
            KeyId.Decimal => "Num .",
            KeyId.Divide => "Num /",
            KeyId.Multiply => "Num *",
            KeyId.Subtract => "Num -",
            KeyId.Add => "Num +",
            KeyId.NumEnter => "Num Enter",
            // 默认使用枚举名
            _ => value.ToString(),
        };
    }



    /// <summary>
    /// 将KeyId转换为System.Windows.Input.Key
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Key ToInputKey(this KeyId value)
    {
        // 部分按键名称相同，使用名称转换
        try
        {
            return Enum.Parse<Key>(value.ToString());
        }
        catch
        {
            // 其他键使用查表法转换
            return value switch
            {
                KeyId.LeftWin => Key.LWin,
                KeyId.RightWin => Key.RWin,
                KeyId.Apostrophe => Key.Oem7,
                KeyId.Comma => Key.OemComma,
                KeyId.Minus => Key.OemMinus,
                KeyId.Equal => Key.OemPlus,
                KeyId.Period => Key.OemPeriod,
                KeyId.Slash => Key.Oem2,
                KeyId.Semicolon => Key.Oem1,
                KeyId.LeftSquareBracket => Key.Oem4,
                KeyId.Backslash => Key.Oem5,
                KeyId.RightSquareBracket => Key.Oem6,
                KeyId.Tilde => Key.Oem3,
                KeyId.Enter => Key.Enter,
                KeyId.ScrollLock => Key.Scroll,
                KeyId.PageUp => Key.Prior,
                KeyId.PageDown => Key.Next,
                KeyId.Backspace => Key.Back,
                KeyId.CapsLock => Key.Capital,
                // None、Unknown和鼠标按键抛异常
                _ => throw new ArgumentOutOfRangeException(nameof(value)),
            };
        }
    }

    /// <summary>
    /// 将KeyId转换为MouseButton
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static MouseButton ToMouseButton(this KeyId value)
    {
        return value switch
        {
            KeyId.MouseLeftButton => MouseButton.Left,
            KeyId.MouseRightButton => MouseButton.Right,
            KeyId.MouseMiddleButton => MouseButton.Middle,
            KeyId.MouseSideButton1 => MouseButton.XButton1,
            KeyId.MouseSideButton2 => MouseButton.XButton2,
            _ => throw new ArgumentOutOfRangeException(nameof(value), "键盘按键请使用ToInputKey方法"),
        };
    }

    /// <summary>
    /// [实验] 将KeyId转换为WinForm中的Keys（用于兼容按键连发功能）
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Keys ToWinFormKeys(this KeyId value)
    {
        try
        {
            return Enum.Parse<Keys>(value.ToInputKey().ToString());
        }
        catch
        {
            return default;
        }
    }



    /// <summary>
    /// 将System.Windows.Input.Key转换为KeyId
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static KeyId FromInputKey(Key value)
    {
        // 部分按键名称相同，使用名称转换
        try
        {
            return Enum.Parse<KeyId>(value.ToString());
        }
        catch
        {
            // 其他键使用查表法转换
            return value switch
            {
                Key.LWin => KeyId.LeftWin,
                Key.RWin => KeyId.RightWin,
                Key.Oem7 => KeyId.Apostrophe,
                Key.OemComma => KeyId.Comma,
                Key.OemMinus => KeyId.Minus,
                Key.OemPlus => KeyId.Equal,
                Key.OemPeriod => KeyId.Period,
                Key.Oem2 => KeyId.Slash,
                Key.Oem1 => KeyId.Semicolon,
                Key.Oem4 => KeyId.LeftSquareBracket,
                Key.Oem5 => KeyId.Backslash,
                Key.Oem6 => KeyId.RightSquareBracket,
                Key.Oem3 => KeyId.Tilde,
                Key.Enter => KeyId.Enter,
                Key.Scroll => KeyId.ScrollLock,
                Key.Prior => KeyId.PageUp,
                Key.Next => KeyId.PageDown,
                Key.Back => KeyId.Backspace,
                Key.Capital => KeyId.CapsLock,
                // 支持列表外的值返回Unknown
                _ => KeyId.Unknown,
            };
        }
    }

    /// <summary>
    /// 将MouseButton转换为KeyId
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static KeyId FromMouseButton(MouseButton value)
    {
        return value switch
        {
            MouseButton.Left => KeyId.MouseLeftButton,
            MouseButton.Right => KeyId.MouseRightButton,
            MouseButton.Middle => KeyId.MouseMiddleButton,
            MouseButton.XButton1 => KeyId.MouseSideButton1,
            MouseButton.XButton2 => KeyId.MouseSideButton2,
            _ => KeyId.Unknown,
        };
    }

}

