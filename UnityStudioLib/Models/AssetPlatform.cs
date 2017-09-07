using System.ComponentModel;

namespace UnityStudio.Models {
    public enum AssetPlatform {

        [Description("Unity Package")]
        UnityPackage = -2,
        Invalid = 0,
        [Description("OS X")]
        Osx = 4,
        [Description("Windows")]
        Windows = 5,
        [Description("Web")]
        Web = 6,
        [Description("Web, streamed)")]
        WebStreamed = 7,
        [Description("iOS")]
        iOS = 9,
        [Description("PlayStation 3")]
        PlayStation3 = 10,
        [Description("XBox 360")]
        Xbox360 = 11,
        [Description("Android")]
        Android = 13,
        [Description("Google NaCl")]
        NaCl = 16,
        [Description("Collab Preview")]
        CollabPreview = 19,
        [Description("Windows Phone 8")]
        WindowsPhone8 = 21,
        [Description("Linux")]
        Linux = 25,
        [Description("Wii U")]
        WiiU = 29

    }
}
