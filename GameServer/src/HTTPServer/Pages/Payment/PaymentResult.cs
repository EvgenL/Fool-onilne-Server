using System;
using System.Collections.Generic;
using System.Linq;
using FoolOnlineServer.HTTPServer.PaymentServices;
using SimpleHttpServer.Models;

namespace FoolOnlineServer.HTTPServer.Pages.Payment {
	public static class PaymentResult {
		public static HttpResponse Get (HttpRequest request) {
			ProcessPayment(request);
			return new HttpResponse() {
				ContentAsUTF8 = "Payment result",
				ReasonPhrase  = "OK",
				StatusCode    = "200"
			};
		}

		/// <summary>
		/// Обработка ответа от платежной системы
		/// </summary>
		/// <param name="request"></param>
		private static void ProcessPayment(HttpRequest request) {
			// Выход если необходимые параметры не были получены
			if (!request.GetParams.ContainsKey("ik_pm_no")) return;
			if (!request.GetParams.ContainsKey("ik_sign")) return;

			// Получение записи об оплате
			Payments.Payment payment = Payments.GetPaymentById(Convert.ToInt64(request.GetParams["ik_pm_no"]));
			if (payment.PaymentId == 0) return; // Выход если запись не найдена

			// Настоящая подпись имеет длину 24 знака
			string receivedSign = request.GetParams["ik_sign"];
			if (receivedSign.Length != 24) return;

			var sortedParams = new SortedDictionary<string, string>(request.GetParams);

			// Убираем подпись
			sortedParams.Remove("ik_sign");

			Dictionary<string, string> parameters     = new Dictionary<string, string>(sortedParams);
			Dictionary<string, string> parametersTest = new Dictionary<string, string>(sortedParams);

			parameters.Add("ik_sign", Interkassa.SecretKey);
			parametersTest.Add("ik_sign", Interkassa.TestSecretKey);

			// Генерация ключа
			var sign     = Interkassa.GetEncodedSign(parameters.Values.ToArray());
			var signTest = Interkassa.GetEncodedSign(parametersTest.Values.ToArray());

			// Если подпись с нормальным и тестовым ключом не совпадает, то выход
			if (signTest != receivedSign && sign != receivedSign) return;

			Payments.UpdateStatus(payment.PaymentId, Payments.Status.Payed); // Обновляем статус

			// Записываем в платеж id из платежной системы
			if (request.GetParams.ContainsKey("ik_inv_id")) {
				long extPaymentId = Convert.ToInt64(request.GetParams["ik_inv_id"]);
				Payments.SetExternalPaymentId(payment.PaymentId, extPaymentId);
			}

			Payments.PayUserMoney(payment.UserId, payment.Sum);
		}
	}
}