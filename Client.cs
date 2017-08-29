using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.IO;

namespace CyreneClient
{
    public class Client
    {
        private string host;
        private string clientId;
        private string clientSecret;
        private string token;
        private string username;
        private string password;
        private string grantType;


        // Standard GrandType is "client_credentials"
        public Client(string clientName, string clientId, string clientSecret)
        {
            if (clientName.StartsWith("http") || clientName.StartsWith("https"))
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(clientName))
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                return;
            }

            host = "https://" + clientName + ".cyrene.io";
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            grantType = "client_credentials";
            token = null;
        }

        // Set user credentials to use GrandType "password"
        public void setUserCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            this.username = username;
            this.password = password;

            grantType = "password";
        }


        public dynamic getEntry(string modelName, string id, string moduleName = "Main")
        {
            ensureToken();

            string relativeUrl = "/" + moduleName + "/" + modelName + "/" + id;
            JToken response = sendProtectedGetRequest(token, relativeUrl);
            
            return response[0];
        }

        public dynamic getEntriesWithFilter(string modelName, String[] filter, string moduleName = "Main")
        {
            string completeFilter = "";
            foreach (string filterName in filter)
            {
                completeFilter += filterName + "&";
            }

            ensureToken();

            string relativeUrl = "/" + moduleName + "/" + modelName + "?" + completeFilter;
            dynamic response = sendProtectedGetRequest(token, relativeUrl);

            return response;
        }


        public bool createEntry(string modelName, Dictionary<string, dynamic> postData, string moduleName = "Main")
        {
            if (postData.Count == 0) return false;

            string jsonPostData = JsonConvert.SerializeObject(postData);

            ensureToken();

            string relativeUrl = "/" + moduleName + "/" + modelName + "/create";
            bool result = sendProtectedPostRequest(token, relativeUrl, jsonPostData);

            return result;
        }

        public bool updateEntry(string modelName, string entryId , Dictionary<string, dynamic> postData, string moduleName = "Main")
        {
            if (postData.Count == 0 || String.IsNullOrEmpty(entryId)) return false;

            string jsonPostData = JsonConvert.SerializeObject(postData);

            ensureToken();

            string relativeUrl = "/" + moduleName + "/" + modelName + "/update/" + entryId;
            bool result = sendProtectedPostRequest(token, relativeUrl, jsonPostData);

            return result;
        }


        public bool testConnection()
        {
            ensureToken();

            if (token == null)
                return false;
            else
                return true;
        }

        private async Task<string> GetToken(HttpContent requestContent)
        {
            using (var client = new HttpClient())
            {
                string tokenUrl = host + "/oauth2/token";
                
                var response = client.PostAsync(tokenUrl, requestContent).Result;
                response.EnsureSuccessStatusCode();

                var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
                var token = payload.Value<string>("access_token");

                this.token = token;
                return token;
            }
        }

        private string getTokenClient()
        {
            HttpContent requestContent = new FormUrlEncodedContent(new Dictionary<string, string> {
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "grant_type", "client_credentials" }
                 });

            string token = GetToken(requestContent).Result;
            return token;
        }

        private string getTokenUser()
        {
            HttpContent requestContent = new FormUrlEncodedContent(new Dictionary<string, string> {
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "username", username },
                    { "password", password },
                    { "grant_type", "password" }
                 });

            string token = GetToken(requestContent).Result;
            return token;
        }

        private void ensureToken()
        {
            if(token == null)
            {
                if (grantType == "client_credentials")
                    getTokenClient();
                else if (grantType == "password")
                    getTokenUser();
            }
        }


        private JToken sendProtectedGetRequest(string token, string relativeUrl)
        {
            using (var client = new HttpClient())
            {
                string fullUrl = host + relativeUrl; // + "&XDEBUG_SESSION_START";
                JToken dataSet = new JObject();

                try
                {
                    HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(fullUrl);
                    myReq.Headers.Add("Authorization", "Bearer " + token);
                    myReq.ContentLength = 0;
                    myReq.ContentType = "application/json";
                    myReq.Headers.Add("X-Requested-With", "XMLHttpRequest");
                    myReq.Accept = "application/json";
                    myReq.Method = WebRequestMethods.Http.Get;
                    var newresponse = myReq.GetResponse();

                    var stream = newresponse.GetResponseStream();
                    StreamReader sr = new StreamReader(stream);
                    var completeString = sr.ReadToEnd();
   
                    JObject jsonObject = JObject.Parse(completeString);
    
                    if ((bool)jsonObject["success"] == true && jsonObject["data"].First.SelectToken("_id") != null)
                    {
                        dataSet = jsonObject["data"];
                    }
                }
                catch
                {
                    //error
                }

                return dataSet;
            }

        }

        private bool sendProtectedPostRequest(string token, string relativeUrl, string postData)
        {
            using (var client = new HttpClient())
            {
                string fullUrl = host + relativeUrl; // + "?XDEBUG_SESSION_START";
                
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

                    HttpResponseMessage newMessage = client.PostAsync(fullUrl, new StringContent(postData, System.Text.Encoding.UTF8, "application/json")).Result;

                    if (!newMessage.IsSuccessStatusCode) return false;
                }
                catch
                {
                    //error
                    return false;
                }

                return true;
            }
        }


        public string getCyreneUrl()
        {
            return host;
        }
    }
}
