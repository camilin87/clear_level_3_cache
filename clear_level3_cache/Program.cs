
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
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

//            var groupId = GetAccessGroupId("288519499", "9TJtJkxW66jXGQS2zS4s");
//            Console.WriteLine(groupId);
//
//            var result = InvalidateCache("288519499", "9TJtJkxW66jXGQS2zS4s", groupId, "sadminmsc.ipcoop.com", "stg.mysubwaycareer.com");
//            Console.WriteLine(result);

            new CacheInvalidator(new ConsoleLogger(), "288519499", "9TJtJkxW66jXGQS2zS4s", "csanchez@ipcoop.com")
                .InvalidateCache("sadminmsc.ipcoop.com", "stg.mysubwaycareer.com");

            Console.Write("Press any key to continue . . . ");
            Console.ReadKey(true);
        }

        private static string GetAccessGroupId(string key, string secretKey)
        {

            string DataFetched = string.Empty;
            string SourceUri = "https://ws.level3.com";
            string ApiPath = "/key/v1.0";
            string Parameters = ""; //parameters if applicable, e.g. ?verbose=true,foo=false
            string FullHttpRequestUri = SourceUri + ApiPath + Parameters;

            HttpWebRequest request = null;
            HttpWebResponse response = null;
            StreamReader readStream = null;
            string HttpStream = string.Empty;

            request = (HttpWebRequest)WebRequest.Create(FullHttpRequestUri);
            request.Timeout = 60 * 1000;

            // One of multiple supported date formats: Tue, 20 Jul 2010 08:49:37 GMT
            string datealt = DateTime.UtcNow.ToString("r");
            string date = DateTime.UtcNow.ToString("ddd, d MMM yyyy HH:mm:ss") + " GMT";
            string contentType = "text/xml";
            string method = "GET";
            string contentMD5 = "";

            // String that will be converted into a signature.
            string signatureString = date + "\n" + ApiPath + "\n" + contentType + "\n" + method + "\n" + contentMD5;
            // generate hash
            HMACSHA1 hmacsha1 = new HMACSHA1(Encoding.ASCII.GetBytes(secretKey));
            byte[] hashValue = hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(signatureString));
            String b64Mac = Convert.ToBase64String(hashValue);
            //for Authorization header
            string auth = "MPA " + key + ":" + b64Mac;

            Type type = request.Headers.GetType();
            MethodInfo mi = type.GetMethod("AddWithoutValidate", BindingFlags.Instance | BindingFlags.NonPublic);
            mi.Invoke(request.Headers, new object[] { "Host", "ws.level3.com" });
            mi.Invoke(request.Headers, new object[] { "Authorization", auth });
            mi.Invoke(request.Headers, new object[] { "Date", date });
            mi.Invoke(request.Headers, new object[] { "Content-Type", contentType });

            response = (HttpWebResponse)request.GetResponse();
            Stream receiveStream = response.GetResponseStream();
            if (receiveStream != null)
            {
                readStream = new StreamReader(receiveStream, Encoding.UTF8);
                HttpStream = readStream.ReadToEnd();
                DataFetched = HttpStream;
            }

            var matchCollection = Regex.Matches(DataFetched, @"accessGroup id=""(\d+)""", RegexOptions.IgnoreCase);
            return matchCollection[0].Groups[1].Value;
        }

        private static bool InvalidateCache(string key, string secretKey, string groupId, params string[] urls)
        {
            string SourceUri = "https://ws.level3.com";
            string ApiPath = "/invalidations/v1.0/" + groupId;
            string Parameters = ""; //parameters if applicable, e.g. ?verbose=true,foo=false
            string FullHttpRequestUri = SourceUri + ApiPath + Parameters;

            HttpWebRequest request = null;
            HttpWebResponse response = null;
            StreamReader readStream = null;
            HttpStatusCode HttpResponseStatusCode = new HttpStatusCode();

            request = (HttpWebRequest)WebRequest.Create(FullHttpRequestUri);
            request.Timeout = 60 * 1000;
            request.Method = "POST";

            var bodyData = BuildBodyData(urls);

            // One of multiple supported date formats: Tue, 20 Jul 2010 08:49:37 GMT
            string datealt = DateTime.UtcNow.ToString("r");
            string date = DateTime.UtcNow.ToString("ddd, d MMM yyyy HH:mm:ss") + " GMT";
            string contentType = "text/xml";
            string method = "POST";
            string contentMD5 = "";

            // String that will be converted into a signature.
            string signatureString = date + "\n" + ApiPath + "\n" + contentType + "\n" + method + "\n" + contentMD5;
            // generate hash
            HMACSHA1 hmacsha1 = new HMACSHA1(Encoding.ASCII.GetBytes(secretKey));
            byte[] hashValue = hmacsha1.ComputeHash(Encoding.ASCII.GetBytes(signatureString));
            String b64Mac = Convert.ToBase64String(hashValue);
            //for Authorization header
            string auth = "MPA " + key + ":" + b64Mac;

            Type type = request.Headers.GetType();
            MethodInfo mi = type.GetMethod("AddWithoutValidate", BindingFlags.Instance | BindingFlags.NonPublic);
            mi.Invoke(request.Headers, new object[] { "Host", "ws.level3.com" });
            mi.Invoke(request.Headers, new object[] { "Authorization", auth });
            mi.Invoke(request.Headers, new object[] { "Date", date });
            mi.Invoke(request.Headers, new object[] { "Content-Type", contentType });

            byte[] bodyDataEncoded = new ASCIIEncoding().GetBytes(bodyData);
            request.ContentLength = bodyDataEncoded.Length;
            request.GetRequestStream().Write(bodyDataEncoded, 0, bodyDataEncoded.Length);

            response = (HttpWebResponse)request.GetResponse();

            return response.StatusCode == HttpStatusCode.OK;
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
    }

    public interface ILogger
    {
        void Log(string message);
    }

    public class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }

    public class CacheInvalidator
    {
        private readonly ILogger logger;
        private readonly string apiKey;
        private readonly string apiSecret;
        private readonly string notificationEmail;

        private const string ApiServer = "ws.level3.com";
        private readonly string SourceUri = $"https://{ApiServer}";
        private const int Timeout = 60 * 1000;

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
            mi.Invoke(request.Headers, new object[] { "Host", ApiServer });
            mi.Invoke(request.Headers, new object[] { "Authorization", GetAuthHeaderValue(dateStr, apiPath, httpVerb, contentType) });
            mi.Invoke(request.Headers, new object[] { "Date", dateStr });
            mi.Invoke(request.Headers, new object[] { "Content-Type", contentType });
        }

        private HttpWebRequest CreateRequest(string apiPath, string method = "GET")
        {
            var Parameters = ""; //parameters if applicable, e.g. ?verbose=true,foo=false
            var fullHttpRequestUri = SourceUri + apiPath + Parameters;

            var request = (HttpWebRequest)WebRequest.Create(fullHttpRequestUri);
            request.Timeout = Timeout;
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
