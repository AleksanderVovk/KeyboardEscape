#if CrazyGamesPlatform_yg && Authorization_yg
using UnityEngine;
using CrazyGames;

namespace YG
{
    public partial class PlatformYG2 : IPlatformsYG2
    {
        public void InitAuth()
        {
            if (CrazySDK.User.IsUserAccountAvailable)
            {
                YG2.player.auth = true;
                YG2.player.name = User.username;
                YG2.player.photo = User.profilePictureUrl;
            }
        }

        public void GetAuth()
        {
            if (CrazySDK.User.IsUserAccountAvailable)
            {
                CrazySDK.User.GetUser(user =>
                {
                    User = user;
                    YG2.GetDataInvoke();
                });
            }
        }

        public void OpenAuthDialog()
        {
            CrazySDK.User.ShowAuthPrompt((error, user) =>
            {
                if (error != null)
                {
                    Debug.LogError("Show auth prompt error: " + error);
                    return;
                }

                User = user;
                InitAuth();
                YG2.GetDataInvoke();
            });
        }
    }
}
#endif