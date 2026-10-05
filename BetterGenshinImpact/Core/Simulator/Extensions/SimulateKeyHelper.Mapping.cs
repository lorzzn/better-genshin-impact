using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.GameTask;
using static Vanara.PInvoke.User32;

namespace BetterGenshinImpact.Core.Simulator.Extensions;

public static partial class SimulateKeyHelper
{
    /// <summary>
    /// 根据默认键位，将脚本中写死的按键映射为实际的按键
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static VK MappingKey(VK source)
    {
        if (TaskContext.Instance().Config.KeyBindingsConfig.GlobalKeyMappingEnabled)
        {
            // NOTE: 普攻、鼠标的冲刺、元素视野、视角居中和派蒙菜单不支持改键，此处不会进行映射
            return source switch
            {
                VK.VK_W => GIActions.MoveForward.ToActionKey().ToVK(),
                VK.VK_S => GIActions.MoveBackward.ToActionKey().ToVK(),
                VK.VK_A => GIActions.MoveLeft.ToActionKey().ToVK(),
                VK.VK_D => GIActions.MoveRight.ToActionKey().ToVK(),
                VK.VK_LCONTROL => GIActions.SwitchToWalkOrRun.ToActionKey().ToVK(),
                VK.VK_E => GIActions.ElementalSkill.ToActionKey().ToVK(),
                VK.VK_Q => GIActions.ElementalBurst.ToActionKey().ToVK(),
                VK.VK_LSHIFT => GIActions.SprintKeyboard.ToActionKey().ToVK(),
                VK.VK_R => GIActions.SwitchAimingMode.ToActionKey().ToVK(),
                VK.VK_SPACE => GIActions.Jump.ToActionKey().ToVK(),
                VK.VK_X => GIActions.Drop.ToActionKey().ToVK(),
                VK.VK_F => GIActions.PickUpOrInteract.ToActionKey().ToVK(),
                VK.VK_Z => GIActions.QuickUseGadget.ToActionKey().ToVK(),
                VK.VK_T => GIActions.InteractionInSomeMode.ToActionKey().ToVK(),
                VK.VK_V => GIActions.QuestNavigation.ToActionKey().ToVK(),
                VK.VK_P => GIActions.AbandonChallenge.ToActionKey().ToVK(),
                VK.VK_1 => GIActions.SwitchMember1.ToActionKey().ToVK(),
                VK.VK_2 => GIActions.SwitchMember2.ToActionKey().ToVK(),
                VK.VK_3 => GIActions.SwitchMember3.ToActionKey().ToVK(),
                VK.VK_4 => GIActions.SwitchMember4.ToActionKey().ToVK(),
                VK.VK_5 => GIActions.SwitchMember5.ToActionKey().ToVK(),
                VK.VK_TAB => GIActions.ShortcutWheel.ToActionKey().ToVK(),
                VK.VK_B => GIActions.OpenInventory.ToActionKey().ToVK(),
                VK.VK_C => GIActions.OpenCharacterScreen.ToActionKey().ToVK(),
                VK.VK_M => GIActions.OpenMap.ToActionKey().ToVK(),
                VK.VK_F1 => GIActions.OpenAdventurerHandbook.ToActionKey().ToVK(),
                VK.VK_F2 => GIActions.OpenCoOpScreen.ToActionKey().ToVK(),
                VK.VK_F3 => GIActions.OpenWishScreen.ToActionKey().ToVK(),
                VK.VK_F4 => GIActions.OpenBattlePassScreen.ToActionKey().ToVK(),
                VK.VK_F5 => GIActions.OpenTheEventsMenu.ToActionKey().ToVK(),
                VK.VK_F6 => GIActions.OpenTheSettingsMenu.ToActionKey().ToVK(),
                VK.VK_F7 => GIActions.OpenTheFurnishingScreen.ToActionKey().ToVK(),
                VK.VK_F8 => GIActions.OpenStellarReunion.ToActionKey().ToVK(),
                VK.VK_J => GIActions.OpenQuestMenu.ToActionKey().ToVK(),
                VK.VK_Y => GIActions.OpenNotificationDetails.ToActionKey().ToVK(),
                VK.VK_RETURN => GIActions.OpenChatScreen.ToActionKey().ToVK(),
                VK.VK_U => GIActions.OpenSpecialEnvironmentInformation.ToActionKey().ToVK(),
                VK.VK_G => GIActions.CheckTutorialDetails.ToActionKey().ToVK(),
                VK.VK_LMENU => GIActions.ShowCursor.ToActionKey().ToVK(),
                VK.VK_L => GIActions.OpenPartySetupScreen.ToActionKey().ToVK(),
                VK.VK_O => GIActions.OpenFriendsScreen.ToActionKey().ToVK(),
                VK.VK_OEM_2 => GIActions.HideUI.ToActionKey().ToVK(),
                // 其他按键（保留的？）不作转换
                _ => source,
            };
        }
        return source;
    }
}
