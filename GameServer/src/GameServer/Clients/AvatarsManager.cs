using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoolOnlineServer.src.GameServer.Clients
{
    public static class AvatarsManager
    {
        public static void SaveAvatar(long connectionId, byte[] avatar)
        {

        }

        public static Image ByteArrayToImage(byte[] imageBytes)
        {
            Image image = null;
            using (MemoryStream stream = new MemoryStream(imageBytes))
            {
                image = Image.FromStream(stream);
            }

            return image;
        }

        public static string ByteArrayFormat(byte[] imageBytes)
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
