using System;

using PlayniteLibraryClient = Playnite.SDK.LibraryClient;

namespace Playnite.SteamFusion.Plugin
{
    public class LibraryClient : PlayniteLibraryClient
    {
        public override bool IsInstalled => true;

        public override void Open()
        {
            throw new NotImplementedException();
        }
    }
}