using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using FoolOnlineServer.AuthServer;
using FoolOnlineServer.Db;

namespace FoolOnlineServer.GameServer.Clients
{
    public static class AvatarsManager
    {

        public static string UploadAvatar(long connectionId, byte[] imageBytes)
        {
            // read path from DB and delete old avatar file
            Client client = ClientManager.GetConnectedClient(connectionId);
            long userId = client.UserData.UserId;
            FoolUser user = DatabaseOperations.GetUserById(userId);
            if (string.IsNullOrEmpty(user.AvatarFile) && File.Exists(user.AvatarFile))
            {
                File.Delete(user.AvatarFile);
            }


            // find out the format of image
            string format = ByteArrayFileFormat(imageBytes);

            // create directory if not exists
            string avatarsFolderName = "avatars"; // todo load from app.config
            Directory.CreateDirectory(avatarsFolderName);

            // write to file
            string filePath = avatarsFolderName + "/" + imageBytes.GetHashCode() + format;

            var stream = File.Create(filePath);
            stream.Write(imageBytes, 0, imageBytes.Length);
            stream.Close();

            // update avatar in db
            DatabaseOperations.UpdateAvatar(userId, filePath);

            // update avatar in buffered client data
            client.UserData.AvatarFile = filePath;

            // return: send avatar exact url on server
            return client.UserData.AvatarFileUrl;
        }

        private static Image ByteArrayToImage(byte[] imageBytes)
        {
            Image image = null;
            using (MemoryStream stream = new MemoryStream(imageBytes))
            {
                image = Image.FromStream(stream);
            }

            return image;
        }

        private static string ByteArrayFileFormat(byte[] imageBytes)
        {
            Image img = AvatarsManager.ByteArrayToImage(imageBytes);
            if (ImageFormat.Jpeg.Equals(img.RawFormat))
            {
                return ".jpeg";
            }
            else if (ImageFormat.Png.Equals(img.RawFormat))
            {
                return ".png";
            }
            else if (ImageFormat.Bmp.Equals(img.RawFormat))
            {
                return ".bmp";
            }
            else
            {
                return ".ERROR";
            }
        }
    }
}
