using System;
using SimpleHttpServer.Models;

namespace FoolOnlineServer.HTTPServer.Pages.Payment {
	public static class PaymentFail {
		public static HttpResponse Get (HttpRequest request) {
			// Выход если необходимые параметры не были получены
			if (request.GetParams.ContainsKey("ik_pm_no")) {
				Payments.Payment payment = Payments.GetPaymentById(Convert.ToInt64(request.GetParams["ik_pm_no"]), Payments.Type.Income);

				if (payment.PaymentId != 0) {
					Payments.UpdateStatus(payment.PaymentId, Payments.Status.Cancelled);

					// Записываем в платеж внешний id из платежной системы
					if (request.GetParams.ContainsKey("ik_inv_id")) {
						long extPaymentId = Convert.ToInt64(request.GetParams["ik_inv_id"]);
						Payments.SetExternalPaymentId(payment.PaymentId, extPaymentId);
					}
				}
			}

			return new HttpResponse() {
				ContentAsUTF8 = "Payment failed",
				ReasonPhrase  = "OK",
				StatusCode    = "200"
			};
		}
	}
}