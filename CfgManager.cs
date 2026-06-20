using BepInEx.Configuration;

namespace ImmersiveRaidTime
{
    public class CfgManager
    {
        public static ConfigEntry<bool> EnableRaidTimeChanger { get; set; }
        public static ConfigEntry<int> UtcOffset { get; private set; }
        public static ConfigEntry<bool> UseDaylightSaving { get; private set; }
        public static ConfigEntry<bool> ImmersiveInfiniteRaid { get; private set; }
        public static ConfigEntry<bool> ShowRealTime { get; private set; }


        public static void Initialize(ConfigFile config)
        {
            EnableRaidTimeChanger = config.Bind(
                "01. 主要设置 / Main Settings",
                "EnableRaidTimeChanger",
                true,
                new ConfigDescription(
                    CfgLocaleManager.Get("cfg_enable_changer_desc"),
                    null,
                    new ConfigurationManagerAttributes { DispName = CfgLocaleManager.Get("cfg_enable_changer_name") }
                )
            );

            ImmersiveInfiniteRaid = config.Bind(
                "01. 主要设置 / Main Settings",
                "ImmersiveInfiniteRaid",
                true,
                new ConfigDescription(
                    CfgLocaleManager.Get("cfg_infinite_raid_desc"),
                    null,
                    new ConfigurationManagerAttributes { DispName = CfgLocaleManager.Get("cfg_infinite_raid_name") }
                )
            );

            UtcOffset = config.Bind(
                "02. 时区配置 / Timezone Settings",
                "UtcOffset",
                8,
                new ConfigDescription(
                    CfgLocaleManager.Get("cfg_utc_offset_desc"),
                    new AcceptableValueRange<int>(-12, 14),
                    new ConfigurationManagerAttributes { DispName = CfgLocaleManager.Get("cfg_utc_offset_name") }
                )
            );

            UseDaylightSaving = config.Bind(
                "02. 时区配置 / Timezone Settings",
                "UseDaylightSaving",
                false,
                new ConfigDescription(
                    CfgLocaleManager.Get("cfg_daylight_saving_desc"),
                    null,
                    new ConfigurationManagerAttributes { DispName = CfgLocaleManager.Get("cfg_daylight_saving_name") }
                )
            );


            // 5. 功能扩展 - 局内O键双显真实时间
            ShowRealTime = config.Bind(
                "03. QOL优化 / QOL Improve",
                "ShowRealTime",
                true,
                new ConfigDescription(
                    CfgLocaleManager.Get("cfg_show_real_time_desc"),
                    null,
                    new ConfigurationManagerAttributes { DispName = CfgLocaleManager.Get("cfg_show_real_time_name") }
                )
            );
            UtcOffset.SettingChanged += (sender, e) => { RaidTimeManager.ApplyDynamicTimeChange(); };
            UseDaylightSaving.SettingChanged += (sender, e) => { RaidTimeManager.ApplyDynamicTimeChange(); };
            EnableRaidTimeChanger.SettingChanged += (sender, e) => { RaidTimeManager.ApplyDynamicTimeChange(); };
            ImmersiveInfiniteRaid.SettingChanged += (sender, e) => { RaidTimeManager.ApplyInfiniteRaidTime(); };
        }
    }
}
