using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;

namespace FoolOnlineServer.HTTPServer.PaymentServices {
	public static class Interkassa {
		private static readonly AppSettingsReader _configReader = new AppSettingsReader();
		public static string SecretKey =>
			(string) _configReader.GetValue("paymentInterkassaSecretKey", typeof(string));
		public static string TestSecretKey =>
			(string) _configReader.GetValue("paymentInterkassaTestSecretKey", typeof(string));
		public static string PaymentId =>
			(string) _configReader.GetValue("paymentInterkassaId", typeof(string));
		public static string PaymentDesc =>
			(string) _configReader.GetValue("paymentInterkassaDesc", typeof(string));


		/// <summary>
		/// Генерация формы оплаты
		/// </summary>
		public static string GetForm(Dictionary<string, string> data) {
			string text = /*"<body onload='document.getElementById(\"payment\").submit();'>" +*/
				"<form id='payment' name='payment' method='post' action='https://sci.interkassa.com/' enctype='utf-8'>";

			foreach (var param in data) {
				if (string.IsNullOrEmpty(param.Value)) continue;
				text += $"<input type='hidden' name='{param.Key}' value='{param.Value}'/>";
			}

			text += "<input type ='submit' value = 'Pay' / >" +
					"</form >"                                +
					"</body>";
			return text;
		}


		/// <summary>
		/// Returns Payment object from reader object
		/// </summary>
		/*public static Payment GetPayment (MySqlDataReader reader) {


			return payment;
		}*/
		public static string GetEncodedSign(string[] data) {
			string signString = string.Join(":", data);
			byte[] md5  = Enc.MD5Bytes(signString);
			string sign = Enc.EncodeBase64Bytes(md5);
			return sign;
		}


		public static Dictionary<string, string> GetDefaultData(long paymentId, double sum) {
			var data = new SortedDictionary<string, string> {
				{"ik_am", sum.ToString(CultureInfo.InvariantCulture)},
				{"ik_co_id", PaymentId},
				{"ik_cur", "RUB"},
				{"ik_desc", PaymentDesc},
				{"ik_pm_no", paymentId.ToString()}
			};

			return new Dictionary<string, string>(data);
		}
	}
}