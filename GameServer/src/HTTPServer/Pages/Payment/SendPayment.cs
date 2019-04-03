using System;
using System.Collections.Generic;
using System.Linq;
using FoolOnlineServer.HTTPServer.PaymentServices;
using SimpleHttpServer;
using SimpleHttpServer.Models;

namespace FoolOnlineServer.HTTPServer.Pages.Payment {
	public static class SendPayment {
		public static HttpResponse Get (HttpRequest request) {
			if (!request.GetParams.ContainsKey("user_id") || !request.GetParams.ContainsKey("sum"))
				return HttpBuilder.NotFound();

			if (!long.TryParse(request.GetParams["user_id"], out var userId)) return HttpBuilder.NotFound();
			if (!float.TryParse(request.GetParams["sum"], out var sum)) return HttpBuilder.NotFound();

			if (userId == 0 || Math.Abs(sum) < 1) return HttpBuilder.NotFound();

			Payments.Payment payment = Payments.CreatePayment(userId, sum, Payments.Type.Income);
			if (payment.PaymentId == 0) return HttpBuilder.NotFound();

			// Получение параметров
			var data = Interkassa.GetDefaultData(payment.PaymentId, payment.Sum);
			// Добавляем в конец параметров ключ по которому будет высчитан хэш
			data.Add("ik_sign", Interkassa.SecretKey);
			var sign = Interkassa.GetEncodedSign(data.Values.ToArray());
			// Заменяем ключ на хэш
			data["ik_sign"] = sign;

			return new HttpResponse() {
				ContentAsUTF8 = Interkassa.GetForm(data),
				ReasonPhrase  = "OK",
				StatusCode    = "200"
			};
		}
	}
}