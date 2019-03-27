using SimpleHttpServer.Models;

namespace FoolOnlineServer.HTTPServer.Pages {
	public static class Home {
		public static HttpResponse Get (HttpRequest request) {
			return new HttpResponse() {
				ContentAsUTF8 = "Hello",
				ReasonPhrase  = "OK",
				StatusCode    = "200"
			};
		}
	}
}