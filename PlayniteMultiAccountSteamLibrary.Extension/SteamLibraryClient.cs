using System;

namespace PlayniteMultiAccountSteamLibrary.Extension
{
    public class SteamLibraryClient : Playnite.SDK.LibraryClient
    {
        public override bool IsInstalled => true;

        public override void Open()
        {
            throw new NotImplementedException();
        }
    }
}