using System.Security.Cryptography;
using System.Text;

namespace FoolOnlineServer.HTTPServer.PaymentServices {
	public static class Enc {
		public static string EncodeBase64Bytes(byte[] bytes) {
			return System.Convert.ToBase64String(bytes);
		}


		public static string EncodeBase64String(string sign) {
			var signBytes = System.Text.Encoding.UTF8.GetBytes(sign);
			return System.Convert.ToBase64String(signBytes);
		}


		public static string DecodeBase64String(string sign) {
			var base64EncodedBytes = System.Convert.FromBase64String(sign);
			return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
		}


		public static string MD5(string text) {
			byte[] hashenc = MD5Bytes(text);
			string result  = "";
			foreach (var b in hashenc) {
				result += b.ToString("x2");
			}
			return result;
		}


		public static byte[] MD5Bytes(string text) {
			byte[] hash    = Encoding.ASCII.GetBytes(text);
			MD5    md5     = new MD5CryptoServiceProvider();
			byte[] hashenc = md5.ComputeHash(hash);
			return hashenc;
		}
	}
}