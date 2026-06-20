using Comfort.Common;
using EFT;
using EFT.UI;
using EFT.UI.BattleTimer;
using EFT.UI.Matchmaker;
using HarmonyLib;
using JsonType;
using System;
using TMPro;
using UnityEngine;

namespace ImmersiveRaidTime
{
    public class RaidTimeManager
    {
        //判断选择的时间点
        public static bool IsInvertedTime = false;
        //缓存战局时间
        private static DateTime? _tempRaidEscapeTime;
        private static TimeSpan? _tempRaidEndTime;
        private static DateTime? _pauseTime;
        //处理时间的工具方法
        public static class TimeHelper
        {
            public static DateTime GetUTCTime() => DateTime.UtcNow;
            public static DateTime GetRealTime() => GetUTCTime().AddHours(CfgManager.UtcOffset.Value).AddHours(CfgManager.UseDaylightSaving.Value ? 1 : 0);
            public static DateTime GetDateTime() => GetRealTime().AddHours(IsInvertedTime ? 12 : 0);
            public static string GetDayTimeString() => GetRealTime().ToString("HH:mm:ss");
            public static string GetNightTimeString() => GetRealTime().AddHours(12).ToString("HH:mm:ss"); //这里是+12还是-12嘞? 需要按时区判定? 反正不显示日期, 应该无所谓吧
            public static DateTime GetTarkovTime() => DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 7).UtcDateTime.AddHours(3).AddHours(IsInvertedTime ? 12 : 0);
            public static bool IsDayTime() => GetDateTime().Hour >= 8 && GetDateTime().Hour < 20;
        }
        //处理日志的工具方法
        public static void ShowError(Exception err)
        {
            Console.WriteLine($"Error: {err.Message}\nStack: {err.StackTrace}");
        }
        //立刻刷新一次时间
        public static void ApplyDynamicTimeChange()
        {
            //空指针
            if (PluginsCore.CorrectGameWorld != null && PluginsCore.CorrectGameWorld.GameDateTime != null)
            {
                //根据天气组件判断是否是工厂
                if (GameObject.Find("Weather") == null)
                {
                    //NotificationManagerClass.DisplayWarningNotification("当前地图（工厂/藏身处）为全室内封闭场景，无法动态调整环境天色。");
                    return;
                }
                //设置战局时间
                try
                {
                    DateTime targetTime = TimeHelper.GetDateTime();
                    if (CfgManager.EnableRaidTimeChanger.Value)
                    {
                        //刷新时间
                        PluginsCore.CorrectGameWorld.GameDateTime.Reset(targetTime, targetTime, 1f);
                        NotificationManagerClass.DisplayMessageNotification($"战局时间流速已同步现实速度。");
                    }
                    else
                    {
                        //恢复时间
                        PluginsCore.CorrectGameWorld.GameDateTime.Reset(TimeHelper.GetUTCTime(), TimeHelper.GetTarkovTime(), 7f);
                        NotificationManagerClass.DisplayMessageNotification($"战局时间流速已恢复游戏速度。");
                    }
                    //Console.WriteLine(PluginsCore.CorrectGameWorld.GameDateTime.DateTime_0.ToString() + PluginsCore.CorrectGameWorld.GameDateTime.DateTime_1.ToString());
                    //NotificationManagerClass.DisplayMessageNotification($"真实时间已同步！当前战局时钟调整为: {targetTime.ToString("HH:mm:ss")}");
                }
                catch (Exception err)
                {
                    ShowError(err);
                }
            }
        }
        //启用无限战局时间
        public static void ApplyInfiniteRaidTime(bool isInitialLoad = false)
        {
            //空指针防御
            if (!Singleton<AbstractGame>.Instantiated) return;
            var gameInstance = Singleton<AbstractGame>.Instance;
            if (gameInstance.GameTimer == null) return;
            //Nullable0、1、2、3分别为战局启动时间、战局结束时间、战局停止的瞬间和战局最大持续时间
            try
            {
                //重定义变量名
                var timer = gameInstance.GameTimer;
                ref var raidStartTime = ref timer.Nullable_0;
                ref var raidEscapeTime = ref timer.Nullable_1;
                ref var raidStopTime = ref timer.Nullable_2;
                ref var raidEndTime = ref timer.Nullable_3;
                //找到右上角的计时器面板
                var mainTimerPanel = UnityEngine.Object.FindObjectOfType<MainTimerPanel>();
                if (CfgManager.EnableRaidTimeChanger.Value && CfgManager.ImmersiveInfiniteRaid.Value)
                {
                    //缓存当前战局倒计时
                    if (_tempRaidEscapeTime == null || _tempRaidEndTime == null)
                    {
                        _tempRaidEscapeTime = raidEscapeTime;
                        _tempRaidEndTime = raidEndTime;
                    }
                    //记录暂停的瞬间
                    //切换到伪无限时长相当于暂停了原有的战局计时器
                    _pauseTime = new DateTime?(DateTime.UtcNow);
                    //无限拉长战局时间
                    //我们的征途是星辰大海！
                    raidEscapeTime = new DateTime?(new DateTime(2200, 1, 1));
                    raidEndTime = raidEscapeTime - raidStartTime;
                    //if (!isInitialLoad) NotificationManagerClass.DisplayMessageNotification("沉浸模式已激活：战局时限已解除，计时器已切换为真实时钟。");
                }
                else
                {
                    //从缓存恢复时间
                    if (_tempRaidEscapeTime == null || _tempRaidEndTime == null) return;
                    //空值合并，防御性判定
                    _pauseTime ??= new DateTime?(DateTime.UtcNow);
                    //计算从暂停到现在过去了多久
                    TimeSpan totalPauseTime = DateTime.UtcNow - _pauseTime.Value;
                    //将启动时间和结束时间向后推迟，即把战局整体时间段向后平移
                    raidEscapeTime = _tempRaidEscapeTime + totalPauseTime;
                    raidEndTime = _tempRaidEndTime + totalPauseTime;
                    //清除缓存
                    _tempRaidEscapeTime = null;
                    _tempRaidEndTime = null;
                    _pauseTime = null;
                    //if (!isInitialLoad) NotificationManagerClass.DisplayMessageNotification("常规战局已恢复：已重连原版撤离倒计时系统。");
                }
                //这个需要清空吗？
                //总之清了
                //gameInstance.GameTimer.Nullable_2 = null;
                //好吧，大概不需要
                //出bug了再加回来就是
                //反射更新倒计时面板
                if (mainTimerPanel != null)
                {
                    //这个不能删，因为恢复时间还得更新一下计时器
                    AccessTools.Field(typeof(TimerPanel), "dateTime_0").SetValue(mainTimerPanel, raidEscapeTime);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
        //让选图界面显示真实时间的Patch
        [HarmonyPatch(typeof(LocationConditionsPanel), "method_1")]
        public static class LocationConditionsPanel_ShowTime_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(LocationConditionsPanel __instance)
            {
                if (__instance == null || !CfgManager.EnableRaidTimeChanger.Value) return;
                try
                {
                    //反射查找必要字段
                    var targetClass = typeof(LocationConditionsPanel);
                    var cureentTime = (TextMeshProUGUI)AccessTools.Field(targetClass, "_currentPhaseTime")?.GetValue(__instance);
                    var inverseTime = (TextMeshProUGUI)AccessTools.Field(targetClass, "_nextPhaseTime")?.GetValue(__instance);
                    if (cureentTime == null) return;
                    bool isChosingMap = (bool)(AccessTools.Field(targetClass, "bool_1")?.GetValue(__instance) ?? false);
                    //无视工厂地图
                    bool isNotFactory = (bool)(AccessTools.Field(targetClass, "bool_0")?.GetValue(__instance) ?? true);
                    EDateTime selectedTimePhase = __instance.SelectedDateTime;
                    //傻逼BSG
                    if (isNotFactory)
                    {
                        //在选图界面
                        if (isChosingMap)
                        {
                            cureentTime.SetMonospaceText(TimeHelper.GetDayTimeString(), true);
                            //神秘，这里还真得分开写，夜晚时钟可能被顺手扬了
                            if (inverseTime != null) inverseTime.SetMonospaceText(TimeHelper.GetNightTimeString(), true);
                        }
                        //二级准备页面
                        else
                        {
                            //根据选择的时间设置真实时间并更新面板
                            string time =(selectedTimePhase == EDateTime.PAST)? TimeHelper.GetNightTimeString(): TimeHelper.GetDayTimeString();
                            cureentTime.SetMonospaceText(time, true);
                        }
                    }
                }
                catch (Exception err)
                {
                    ShowError(err);
                }
            }
        }
        //捕获玩家选择白图还是夜图的Patch
        [HarmonyPatch(typeof(LocationConditionsPanel), "Set")]
        public static class LocationConditionsPanel_Set_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(LocationConditionsPanel __instance, ref RaidSettings raidSettings)
            {
                if (__instance == null || raidSettings?.SelectedLocation == null || !CfgManager.EnableRaidTimeChanger.Value) return;
                //去你妈的
                if (!raidSettings.SelectedLocation.Id.Contains("factory"))
                {
                    //从raidSettings直接捕获时间选择器
                    IsInvertedTime = raidSettings.SelectedDateTime != EDateTime.CURR;
                }
            }
        }
        //修改战局计时器显示的Patch
        [HarmonyPatch(typeof(TimerPanel), "SetTimerText")]
        public class TimerPanel_SetTimerText_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(TimerPanel __instance, ref TimeSpan timeSpan)
            {
                if (CfgManager.EnableRaidTimeChanger.Value && CfgManager.ImmersiveInfiniteRaid.Value)
                {
                    //我们只修改战局计时器，而不修改其它的计时器面板
                    if (__instance is MainTimerPanel)
                    {
                        //一键搞定
                        timeSpan = TimeHelper.GetDateTime().TimeOfDay;
                    }
                }
            }
        }
        //在开始游戏时刷新时间设置的Patch
        [HarmonyPatch(typeof(GameWorld), "OnGameStarted")]
        public class GameWorld_OnGameStarted_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(GameWorld __instance)
            {
                if (__instance == null) return;
                //Console.WriteLine("LocationID: " + __instance.LocationId);
                //直接过滤掉工厂
                if (CfgManager.EnableRaidTimeChanger.Value && __instance.GameDateTime != null && __instance.LocationId != "factory4_night" && __instance.LocationId != "factory4_day")
                {
                    //立刻刷新一次时间（它其实没用了，但我决定留着它）
                    //算了，还是删掉吧
                    //__instance.GameDateTime.Reset(RealTimeHelper.GetDateTime(), RealTimeHelper.GetDateTime(), 1f);
                }
                //初始化沉浸战局
                ApplyInfiniteRaidTime(true);
            }
        }
        //在地图加载完成前应用时间修改的Patch
        [HarmonyPatch(typeof(BaseLocalGame<EftGamePlayerOwner>), "method_3")]
        public static class LocalGame_InitTime_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(object __instance)
            {
                if (!CfgManager.EnableRaidTimeChanger.Value) return;
                try
                {
                    var targetType = __instance.GetType();
                    //反射读取地图ID
                    var location = AccessTools.Field(targetType, "Location_0")?.GetValue(__instance) as LocationSettingsClass.Location;
                    if (location != null && location.Id.Contains("factory")) return;
                    //反射读取时间控制器
                    var gameDateTime = AccessTools.Property(targetType, "GameDateTime")?.GetValue(__instance) as GameDateTime;
                    if (gameDateTime != null)
                    {
                        //设置为当前时间
                        DateTime targetTime = TimeHelper.GetDateTime();
                        gameDateTime.Reset(targetTime, targetTime, 1f, force: true);
                        //Console.WriteLine($"[RealRaidTime] 战局控制器时间已成功接管为: {targetTime}");
                    }
                }
                catch (Exception err)
                {
                    ShowError(err);
                }
            }
        }
        //修改单击O键的QOL时间显示结果的Patch
        [HarmonyPatch(typeof(LocationTimeUIPanel), "method_0")]
        public class LocationTimeUIPanel_ShowTime_Patch
        {
            [HarmonyPostfix]
            static void Postfix(LocationTimeUIPanel __instance, DateTime timeOfDay)
            {

                var timePanel = (TextMeshProUGUI)AccessTools.Field(typeof(LocationTimeUIPanel), "_timePanel").GetValue(__instance);
                if (timePanel == null) return;
                //获取玩家的现实时间和战局时间
                string realTimeText = DateTime.Now.ToString("HH:mm");
                string raidTimeText = timeOfDay.ToString("HH:mm");
                //判断地图
                bool isFactory = false;
                if (Singleton<GameWorld>.Instantiated && !string.IsNullOrEmpty(Singleton<GameWorld>.Instance.LocationId))
                {
                    isFactory = Singleton<GameWorld>.Instance.LocationId.ToLower().Contains("factory");
                }
                //默认为战局时间
                string finalText = raidTimeText;
                //开启沉浸战局下，这里只会显示时间图标，不会显示时间
                //未开启沉浸战局且开启显示真实时间，则工厂会显示真实时间，其余地图会显示战局时间 | 真实时间
                //未开启沉浸战局且未开启显示真实时间，则按照原版逻辑（工厂不显示时间，其余地图显示时间）
                if (!CfgManager.ImmersiveInfiniteRaid.Value)
                {
                    if (CfgManager.ShowRealTime.Value)
                    {
                        finalText = isFactory ? realTimeText : $"{raidTimeText} | {realTimeText}";
                    }
                    else
                    {
                        finalText = isFactory ? "" : raidTimeText;
                    }
                }
                else
                {
                    //沉浸式战局只在启用时间同步才有效
                    if(CfgManager.EnableRaidTimeChanger.Value) finalText = "";
                }
                //覆盖文本
                timePanel.SetText(finalText);
            }
        }
        //劫持QOL时间显示的Patch
        [HarmonyPatch(typeof(LocationTimeUIPanel), "Initialize")]
        public class LocationTimeUIPanel_Initialize_Patch
        {
            [HarmonyPostfix]
            static void Postfix(LocationTimeUIPanel __instance)
            {
                //反射获取时间面板
                TextMeshProUGUI timePanel = (TextMeshProUGUI)AccessTools.Field(typeof(LocationTimeUIPanel), "_timePanel").GetValue(__instance);
                if (timePanel != null)
                {
                    //强制显示面板
                    timePanel.gameObject.SetActive(true);
                }
            }
        }
    }
}
