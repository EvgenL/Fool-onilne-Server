using System;
using System.Collections.Generic;
using System.Linq;
using FoolOnlineServer.HTTPServer.Pages.Avatars;
using SimpleHttpServer;
using SimpleHttpServer.Models;
using SimpleHttpServer.RouteHandlers;

namespace FoolOnlineServer.HTTPServer {
	public static class Routes {
		public static List<Route> GET =>
			new List<Route>() {
				// Главная страница
				new Route() {
					Callable = Pages.Home.Get,
					UrlRegex = "^\\/$",
					Method   = "GET"
				},
				// Страница с которой идет перенаправление на платежную систему
				new Route() {
					Callable = Pages.Payment.SendPayment.Get,
					UrlRegex = "^\\/payment\\/(.*)$",
					Method   = "GET"
				},
				// Страница, которая обрабатывает результат от платежной системы
				new Route() {
					Callable = Pages.Payment.PaymentResult.Get,
					UrlRegex = "^\\/payment_result\\/(.*)$",
					Method   = "GET"
				},
				// Сюда попадает юзер после успешной оплаты
				new Route() {
					Callable = Pages.Payment.PaymentSuccess.Get,
					UrlRegex = "^\\/payment_success\\/(.*)$",
					Method   = "GET"
				},
			    // Сюда попадает при неудачной оплате
			    new Route() {
			        Callable = Pages.Payment.PaymentFail.Get,
			        UrlRegex = "^\\/payment_fail\\/(.*)$",
			        Method   = "GET"
			    },

			    // Скачивание аватарки
			    new Route() {
			        Callable = Pages.Avatars.AvatarFile.Get,
			        UrlRegex = "^\\/avatars\\/(.*)$",
			        Method   = "GET"
			    },
				/*new Route() {
						Callable = Static,
						UrlRegex = "^\\/Static\\/(.*)$",
						Method   = "GET"
					}*/
				/*new Route()
					{
						Callable = new FileSystemRouteHandler() { BasePath = @"C:\Users\Barend.Erasmus\Desktop\Test"}.Handle,
						UrlRegex = "^\\/Static\\/(.*)$",
						Method   = "GET"
					}*/
			};
	}
}