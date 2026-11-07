#if CrazyGamesPlatform_yg && PlayerStats_yg
using CrazyGames;

namespace YG
{
    public partial class PlatformYG2 : IPlatformsYG2
    {
        public void SetString(string key, string value) => CrazySDK.Data.SetString(key, value);
        public string GetString(string key) => CrazySDK.Data.GetString(key);
        public string GetString(string key, string defaultValue) => CrazySDK.Data.GetString(key, defaultValue);

        public void SetFloat(string key, float value) => CrazySDK.Data.SetFloat(key, value);
        public float GetFloat(string key) => CrazySDK.Data.GetFloat(key);
        public float GetFloat(string key, float defaultValue) => CrazySDK.Data.GetFloat(key, defaultValue);

        public void SetInt(string key, int value) => CrazySDK.Data.SetInt(key, value);
        public int GetInt(string key) => CrazySDK.Data.GetInt(key);
        public int GetInt(string key, int defaultValue) => CrazySDK.Data.GetInt(key, defaultValue);

        public bool HasKey(string key) => CrazySDK.Data.HasKey(key);
        public void DeleteKey(string key) => CrazySDK.Data.DeleteKey(key);
        public void DeleteAll() => CrazySDK.Data.DeleteAll();
    }
}
#endif