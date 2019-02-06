namespace FoolOnlineServer.GameServer
{
    //Static parameters set on launch //TODO set on launch
    public static class StaticParameters
    {
        //String for mysql database connetion
        public static string ConnetionString = "server=localhost;uid=admin;pwd=admin;database=foolonline";
        //Max clients connected
        public static long MaxClients = 1500;

    }
}
