using SimpleHttpServer.Models;

namespace FoolOnlineServer.HTTPServer.Pages.Payment {
	public static class PaymentSuccess {
		public static HttpResponse Get (HttpRequest request) {
			return new HttpResponse() {
				ContentAsUTF8 = "Payment success!",
				ReasonPhrase  = "OK",
				StatusCode    = "200"
			};
		}
	}
}