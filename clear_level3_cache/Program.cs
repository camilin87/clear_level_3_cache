
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace API_Sample
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Executing API Test");

           new CacheInvalidator(new ConsoleLogger(), "288519499", "9TJtJkxW66jXGQS2zS4s", "csanchez@ipcoop.com")
                .InvalidateCache("sadminmsc.ipcoop.com", "stg.mysubwaycareer.com");

            Console.Write("Press any key to continue . . . ");
            Console.ReadKey(true);
        }
    }


    public class ConsoleLogger : CacheInvalidator.ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }

    public class CacheInvalidator
    {
        public interface ILogger
        {
            void Log(string message);
        }

        private readonly ILogger logger;
        private readonly string apiKey;
        private readonly string apiSecret;
        private readonly string notificationEmail;

        private const string API_SERVER = "ws.level3.com";
        private readonly string sourceUri = $"https://{API_SERVER}";
        private const int TIMEOUT = 60 * 1000;

        public CacheInvalidator(ILogger logger, string apiKey, string apiSecret, string notificationEmail)
        {
            this.logger = logger;
            this.apiKey = apiKey;
            this.apiSecret = apiSecret;
            this.notificationEmail = notificationEmail;
        }

        public void InvalidateCache(params string[] websiteUrls)
        {
            logger.Log($"InvalidateCache Key={apiKey}, Secret={apiSecret}, Notification={notificationEmail}");

            var groupId = GetAccessGroupId();
            logger.Log($"AccessGroupId={groupId}");
            if (string.IsNullOrEmpty(groupId)) throw new InvalidOperationException("Error Getting GroupId");

            foreach (var url in websiteUrls)
            {
                logger.Log($"Invalidating Url={url}");
            }

            var invalidationResult = InvalidateProperties(groupId, websiteUrls);
            logger.Log($"InvalidationResult={invalidationResult}");
            if (!invalidationResult) throw new InvalidOperationException("Invalidation Failed");
        }

        private string GetAccessGroupId()
        {
            var apiPath = "/key/v1.0";
            var request = CreateRequest(apiPath);
            var dateStr = GetDateStr();
            AddMandatoryHeaders(request, dateStr, apiPath, "GET");

            var response = (HttpWebResponse)request.GetResponse();
            var readStream = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            var responseBody = readStream.ReadToEnd();

            var matchCollection = Regex.Matches(responseBody, @"accessGroup id=""(\d+)""", RegexOptions.IgnoreCase);
            return matchCollection[0].Groups[1].Value;
        }

        private bool InvalidateProperties(string groupId, IEnumerable<string> urls)
        {
            var apiPath = "/invalidations/v1.0/" + groupId;
            var request = CreateRequest(apiPath, "POST");
            var dateStr = GetDateStr();
            AddMandatoryHeaders(request, dateStr, apiPath, "POST");
            WriteRequestBody(request, BuildBodyData(urls));

            var response = (HttpWebResponse)request.GetResponse();
            return response.StatusCode == HttpStatusCode.OK;
        }

        private void WriteRequestBody(HttpWebRequest request, string body)
        {
            var bodyDataEncoded = new ASCIIEncoding().GetBytes(body);
            request.ContentLength = bodyDataEncoded.Length;
            request.GetRequestStream().Write(bodyDataEncoded, 0, bodyDataEncoded.Length);
        }

        private static string BuildBodyData(IEnumerable<string> urls)
        {
            return string.Join(string.Empty, BuildBodyDataInternal(urls));
        }
        private static IEnumerable<string> BuildBodyDataInternal(IEnumerable<string> urls)
        {
            yield return "<properties>";
            var allProperties = urls.Select(u => $"<property><name>{u}</name><paths><path>/*</path></paths></property>");
            yield return string.Join(string.Empty, allProperties);
            yield return "</properties>";
        }

        private void AddMandatoryHeaders(HttpWebRequest request, string dateStr, string apiPath, string httpVerb, string contentType = "text/xml")
        {
            var mi = request.Headers.GetType().GetMethod("AddWithoutValidate", BindingFlags.Instance | BindingFlags.NonPublic);
            mi.Invoke(request.Headers, new object[] { "Host", API_SERVER });
            mi.Invoke(request.Headers, new object[] { "Authorization", GetAuthHeaderValue(dateStr, apiPath, httpVerb, contentType) });
            mi.Invoke(request.Headers, new object[] { "Date", dateStr });
            mi.Invoke(request.Headers, new object[] { "Content-Type", contentType });
        }

        private HttpWebRequest CreateRequest(string apiPath, string method = "GET")
        {
            var parameters = $"?notification={notificationEmail}";
            var fullHttpRequestUri = sourceUri + apiPath + parameters;

            var request = (HttpWebRequest)WebRequest.Create(fullHttpRequestUri);
            request.Timeout = TIMEOUT;
            request.Method = method;

            return request;
        }

        private string GetDateStr()
        {
            return DateTime.UtcNow.ToString("ddd, d MMM yyyy HH:mm:ss") + " GMT";
        }

        private string GetAuthHeaderValue(string dateStr, string apiPath, string method, string contentType, string contentMD5 = "")
        {
            var signatureString = dateStr + "\n" + apiPath + "\n" + contentType + "\n" + method + "\n" + contentMD5;
            // generate hash
            var hashValue = new HMACSHA1(Encoding.ASCII.GetBytes(apiSecret))
                .ComputeHash(Encoding.ASCII.GetBytes(signatureString));
            var b64Mac = Convert.ToBase64String(hashValue);
            //for Authorization header
            return "MPA " + apiKey + ":" + b64Mac;
        }
    }
}
