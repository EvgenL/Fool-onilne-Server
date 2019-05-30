using FoolOnlineServer.AuthServer;

namespace FoolOnlineServer.Db
{
    /// <summary>
    /// Object returned by database
    /// </summary>
    public class FoolUser
    {
        public long UserId;
        public string Nickname;
        public string Password;
        public string Email;
        public double Money;
        public string AvatarFile;

        public string AvatarFileUrl => 
        string.IsNullOrEmpty(AvatarFile) ?
        "" : 
            "http://" +
              AccountsServer.GameServerIp + ":" + HTTPServer.HTTPServer.Port +
              "/" + AvatarFile;
    }
}
