using SimpleHttpServer.Models;
using System.IO;

namespace FoolOnlineServer.HTTPServer.Pages.Avatars
{
    class AvatarFile
    {
        public static HttpResponse Get(HttpRequest request)
        {
            // if url starts with slash (it probably does) then delete slash
            var path = request.Url.StartsWith("/") ? request.Url.Substring(1) : request.Url;

            // if avatar exists - send it to client
            HttpResponse response = null;
            if (File.Exists(path))
            {
                response = new HttpResponse()
                {
                    Content = File.ReadAllBytes(path),
                    ReasonPhrase = "OK",
                    StatusCode = "200"
                };

                string format = path.Substring(path.LastIndexOf(".") + 1);

                response.Headers.Add("Content-Type", "image/" + format);
            }
            else
            {
                response = new HttpResponse()
                {
                    ContentAsUTF8 = "No such avatar",
                    ReasonPhrase = "OK",
                    StatusCode = "404"
                };
            }


            return response;
        }
    }
}
