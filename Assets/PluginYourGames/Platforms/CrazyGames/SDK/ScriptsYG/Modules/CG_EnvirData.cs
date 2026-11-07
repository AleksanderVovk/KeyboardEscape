#if CrazyGamesPlatform_yg && EnvirData_yg
using CrazyGames;

namespace YG
{
    public partial class PlatformYG2 : IPlatformsYG2
    {
        public void InitEnirData()
        {
            if (CrazySDK.User.IsUserAccountAvailable)
            {
                YG2.envir.language = CrazySDK.User.SystemInfo.countryCode.ToLower();
                YG2.envir.browser = CrazySDK.User.SystemInfo.browser.name;
                YG2.envir.platform = CrazySDK.User.SystemInfo.os.name;

                string device = CrazySDK.User.SystemInfo.device.type;
                YG2.envir.deviceType = device;

                switch (device)
                {
                    case "desktop":
                        YG2.envir.isDesktop = true;
                        break;
                    case "tablet":
                        YG2.envir.isTablet = true;
                        break;
                    case "mobile":
                        YG2.envir.isMobile = true;
                        break;
                }
            }
        }

        public void GetEnvirData()
        {
            InitEnirData();
            YG2.GetDataInvoke();
        }
    }
}
#endif
