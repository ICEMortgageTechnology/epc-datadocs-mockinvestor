using System.Collections.Generic;
using EPC.DataDocs.MockInvestor.Models;
using Newtonsoft.Json.Linq;
using EPC.DataDocs.MockInvestor.Helpers;
using EPC.DataDocs.MockInvestor.Wrappers.Interfaces;
using Microsoft.Extensions.Options;

/*
* Copyright 2017 Ellie Mae, Inc.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are met:
*
*  1. Redistributions of source code must retain the above copyright notice,
*     this list of conditions and the following disclaimer.
*
*  2. Redistributions in binary form must reproduce the above copyright notice,
*     this list of conditions and the following disclaimer in the documentation
*     and/or other materials provided with the distribution.
*
*  3. Neither the name of the copyright holder nor the names of its
*     contributors may be used to endorse or promote products derived from this
*     software without specific prior written permission.
*
* THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
* AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
* IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
* ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
* LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
* CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
* SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
* INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
* CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
* ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
* POSSIBILITY OF SUCH DAMAGE.
*/

namespace EPC.DataDocs.MockInvestor.Wrappers
{
    public class OAuthWrapper : IOAuthWrapper
    {
        private IEnumerable<KeyValuePair<string, string>> _OAuthCredentials = null;
        private string _OAuthURL = string.Empty;
        private string _EndPoint = string.Empty;

        /// <summary>
        /// Public constructor that takes OAuthKeys model
        /// </summary>
        /// <param name="oAuthKeys"></param>
        public OAuthWrapper(Dictionary<string, string> oAuthKeys, string oAuthURL, string endPoint)
        {
            _OAuthCredentials = oAuthKeys;

            if (!string.IsNullOrEmpty(oAuthURL))
                this._OAuthURL = oAuthURL;
            //this._OAuthURL = string.Format("https://{0}", oAuthURL);

            if (!string.IsNullOrEmpty(endPoint))
                this._EndPoint = endPoint;
        }

        /// <summary>
        /// Gets the OAuth Access token based on the credentials passed
        /// </summary>
        /// <returns></returns>
        public JwtToken GetOAuthAccessToken()
        {
            JwtToken responseToken = null;

            // getting the response string by performing an Http Post
            //var responseString = HttpCommunicationHelper.PerformPost(_OAuthCredentials, this._OAuthURL, this._EndPoint);
            var responseString = HttpCommunicationHelper.ExecuteRequest(_OAuthCredentials, this._OAuthURL, this._EndPoint, RestSharp.Method.POST);

            if (!string.IsNullOrEmpty(responseString))
            {
                var jsonObject = JObject.Parse(responseString);

                // parsing the response if it is not null
                if (jsonObject != null)
                {
                    responseToken = new JwtToken()
                    {
                        TokenString = jsonObject.GetValue("access_token").ToString(),
                        TokenType = jsonObject.GetValue("token_type").ToString()
                    };
                }
            }

            return responseToken;
        }

        /// <summary>
        /// Will return the header that is required for generating the OAuth Token from OAPI
        /// </summary>
        /// <param name="appSettings"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetOAuthHeaders(IOptions<AppSettings> appSettings)
        {
            var OAuthKeyDictionary = new Dictionary<string, string>();

            if (appSettings != null)
            {
                //OAuthKeyDictionary.Add("api_host", appSettings.APIHost);
                OAuthKeyDictionary.Add("client_id", appSettings.Value.Token.ClientID);
                OAuthKeyDictionary.Add("client_secret", appSettings.Value.Token.ClientSecret);
                OAuthKeyDictionary.Add("grant_type", "client_credentials");
                OAuthKeyDictionary.Add("scope", appSettings.Value.Token.Scope);
            }

            return OAuthKeyDictionary;
        }

        /// <summary>
        /// Will Introspect the token
        /// </summary>
        /// <returns></returns>
        public JObject IntrospectAccessToken()
        {
            // getting the response string by performing an Http Post
            var responseString = HttpCommunicationHelper.ExecuteRequest(_OAuthCredentials, this._OAuthURL, this._EndPoint, RestSharp.Method.POST);

            var jsonObject = JObject.Parse(responseString);

            return jsonObject;
        }
    }
}
