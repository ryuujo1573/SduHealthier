using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using HtmlAgilityPack;
using static SduHealthier.Core.Utilities.Encrypting;

namespace SduHealthier.Core
{
    public class HealthAutoHelper
    {
        private readonly string _studentId;
        private readonly string _password;
        private readonly string _loginCookie;
        private readonly bool _isHome;
        
        private string ServiceId => _isHome ? HomeServiceId : SchoolServiceId;

        private const string HomeServiceId = "41d9ad4a-f681-4872-a400-20a3b606d399";
        private const string SchoolServiceId = "e027d752-0cbc-4d83-a9d5-1692441e8252";

        private readonly HttpClient _client;
        private string _formId;
        private string _processId;
        private string _privilegeId;
        private string _sysFk = string.Empty;

        // Use `Resources` directly.
        // private string LoginUrl => Resources.LoginUrl;
        // private const string ServiceUrl = "https://service.sdu.edu.cn/tp_up";
        // private const string HomeUrl = "https://service.sdu.edu.cn/tp_up/view?m=up#act=portal/viewhome";
        // private const string ServeApplyUrl = "https://scenter.sdu.edu.cn/tp_fp/view?m=fp#from=hall&serveID=41d9ad4a-f681-4872-a400-20a3b606d399&act=fp/serveapply";
        public HealthAutoHelper(string studentId, string password, bool isHome = false)
        {
            _studentId = studentId;
            _password = password;
            _isHome = isHome;

            var provider = new RNGCryptoServiceProvider();
            const int length = 16;
            var randomBytes = new byte[length];
            provider.GetBytes(randomBytes);

            _loginCookie = string.Create(length * 2, (value: randomBytes, startIndex: 0, length),
                static(dst, state) =>
                {
                    var src = new ReadOnlySpan<byte>(state.value, state.startIndex, state.length);

                    var i = 0;
                    var j = 0;

                    // output the char
                    while (i < src.Length)
                    {
                        var b = src[i++];
                        dst[j++] = ToCharUpper(b >> 4);
                        dst[j++] = ToCharUpper(b);
                    }
                });

            // var matches = Regex.Matches(Resources.UserAgent,
            //     @"(?<product>\w+)/(?<version>.+)\s?(?<comment>\([\s\S]*\))?");
            // var agents = matches.Select(match =>
            // {
            //     match.Groups["product"] = 
            // })
            _client = new HttpClient();
            _client.DefaultRequestHeaders.UserAgent.TryParseAdd(Resources.UserAgent);
        }

        // Note: this method may be (and should be) converted to an inline function.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char ToCharUpper(int value)
        {
            value &= 0xF;
            value += '0';

            if (value > '9')
            {
                value += ('a' - ('9' + 1));
            }

            return (char) value;
        }

        private async Task Login()
        {
            try
            {
                var loginResponse = await _client.GetAsync(Resources.LoginUrl);
                if (!loginResponse.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        loginResponse.Content.ToString(),
                        new Exception("Login Error"),
                        loginResponse.StatusCode);
                }

                var htmlDoc = new HtmlDocument();
                htmlDoc.Load(await loginResponse.Content.ReadAsStreamAsync());

                var lt = htmlDoc.GetElementbyId("lt").Attributes["value"].Value;
                if (lt is null)
                {
                    throw new Exception("\"#lt@value\" not found");
                }

                var execution = htmlDoc.DocumentNode.SelectSingleNode("//input[@name=\"execution\"]")
                    .Attributes["value"].Value;
                var fakeRsa = GenerateRsa($"{_studentId}{_password}{lt}");

                IEnumerable<KeyValuePair<string?, string?>> requestBody = new Dictionary<string, string>
                {
                    {"rsa", fakeRsa},
                    {"ul", _studentId.Length.ToString()},
                    {"pl", _password.Length.ToString()},
                    {"lt", lt},
                    {"execution", execution},
                    {"_eventId", "submit"}
                }.ToArray().Cast<KeyValuePair<string?, string?>>();

                loginResponse = await _client.PostAsync(Resources.LoginUrl, new FormUrlEncodedContent(requestBody));
                if (loginResponse.IsSuccessStatusCode)
                {
                    var homeResponse = await _client.GetAsync(Resources.ServiceHomeUrl);
                    htmlDoc.Load(await homeResponse.Content.ReadAsStringAsync());
                    var title = htmlDoc.DocumentNode.SelectSingleNode("title").InnerText;

                    if (title is null)
                    {
                        // todo: finish this
                        throw new Exception("");
                    }

                    const string expectedTitle = "山东大学信息化公共服务平台";
                    if (title != expectedTitle)
                    {
                        throw new WarningException($"Is title changed?\n\tExpected: {expectedTitle}, Actual: {title}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isHome">打卡模块; true: 离校生, false: 在校生</param>
        private async Task CheckIn()
        {
            // GET / service
            var getServiceResponse = await _client.GetAsync(
                string.Format(Resources.ServeApplyUrlTemp, ServiceId));
            if (!getServiceResponse.IsSuccessStatusCode)
            {
                // todo
                throw new Exception("getServiceResponse Error");
            }
            
            // Service common:
            // ** Due to the .net issue, casting is quite messy.
            // var serviceHeaders = new Dictionary<string, string>
            // {
            //     {"Content-Type", "application/json"},
            //     {"Origin", new Uri(Resources.ServeBaseUrl).Let(uri => $"{uri.Scheme}://{uri.Host}")},
            //     {"X-Requested-With", "XMLHttpRequest"},
            //     {"Referer", Resources.ServeBaseUrl}
            // };
            
            // POST / check_service
            var body = new {serveId = ServiceId};
            var checkMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(Resources.CheckServiceUrl),
                Content = new StringContent(JsonConvert.SerializeObject(body)),
                Headers = {
                    {"Content-Type", "application/json"},
                    {"Origin", new Uri(Resources.ServeBaseUrl).Let(uri => $"{uri.Scheme}://{uri.Host}")},
                    {"X-Requested-With", "XMLHttpRequest"},
                    {"Referer", Resources.ServeBaseUrl}
                }
            };
            var postCheckResponse = await _client.SendAsync(checkMessage);
            if (!postCheckResponse.IsSuccessStatusCode)
            {
                // todo
                throw new Exception("postCheckResponse Error");
            }
            
            // POST / serve_info
            var body2 = new {serviceID = ServiceId};
            var serveMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(Resources.ServeInfoUrl),
                Content = new StringContent(JsonConvert.SerializeObject(body2)),
                Headers =
                {
                    {"Content-Type", "application/json"},
                    {"Origin", new Uri(Resources.ServeBaseUrl).Let(uri => $"{uri.Scheme}://{uri.Host}")},
                    {"X-Requested-With", "XMLHttpRequest"},
                    {"Referer", Resources.ServeBaseUrl}
                }
            };
            var postServeResponse = await _client.SendAsync(serveMessage);
            if (!postServeResponse.IsSuccessStatusCode)
            {
                // todo
                throw new Exception("postServeResponse Error");
            }
            
            // POST / get_serve_apply
            var body3 = new {serveID = ServiceId, from = "hall"};
            var applyMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(Resources.ServeInfoUrl),
                Content = new StringContent(JsonConvert.SerializeObject(body3)),
                Headers =
                {
                    {"Content-Type", "application/json"},
                    {"Origin", new Uri(Resources.ServeBaseUrl).Let(uri => $"{uri.Scheme}://{uri.Host}")},
                    {"X-Requested-With", "XMLHttpRequest"},
                    {"Referer", Resources.ServeBaseUrl}
                }
            };
            var postApplyResponse = await _client.SendAsync(applyMessage);
            if (!postApplyResponse.IsSuccessStatusCode)
            {
                // todo
                throw new Exception("postApplyResponse Error");
            }

            var jsonText = await postApplyResponse.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<dynamic>(jsonText);
            if (json == null)
            {
                throw new Exception("postApplyResponse Json Error");
            }
            _formId = json.formID;
            _processId = json.procID;
            _privilegeId = json.privilegeId;
            
            // POST / continue_service
            var body4 = new {serviceID = ServiceId};
            var continueMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(Resources.ServeInfoUrl),
                Content = new StringContent(JsonConvert.SerializeObject(body4)),
                Headers =
                {
                    {"Content-Type", "application/json"},
                    {"Origin", new Uri(Resources.ServeBaseUrl).Let(uri => $"{uri.Scheme}://{uri.Host}")},
                    {"X-Requested-With", "XMLHttpRequest"},
                    {"Referer", Resources.ServeBaseUrl}
                }
            };
            var postContinueResponse = await _client.SendAsync(continueMessage);
            if (!postContinueResponse.IsSuccessStatusCode)
            {
                throw new Exception("postContinueResponse Error");
            }
            
            // GET / sign_data
            var url = new Uri(Resources.ServeBaseUrl)
                .Let(uri => $"{uri.Scheme}://{uri.Host}/tp_fp/formParser?status=select&formid={_formId}&service_id={ServiceId}&process={_processId}&seqId={null}&SYS_FK={_sysFk}&privilegeId={_privilegeId}");
            var getDataResponse = await _client.GetAsync(url);
            if (!getDataResponse.IsSuccessStatusCode)
            {
                throw new Exception("getDataResponse Error");
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.Load(await getDataResponse.Content.ReadAsStreamAsync());
            var lastData = htmlDoc.DocumentNode.SelectSingleNode("#dcstr").InnerText;
            if (string.IsNullOrEmpty(lastData))
            {
                throw new Exception("XPath not found: #dcstr");
            }

            var lastDataObject = JsonConvert.DeserializeAnonymousType(
                lastData, new { body = new {} });
        }
    }
}