using System;
using System.Collections.Generic;
using EPC.DataDocs.MockInvestor.Wrappers.Interfaces;
using EPC.DataDocs.MockInvestor.Models;
using Microsoft.Extensions.Options;
using System.Net;
using Newtonsoft.Json;
using RestSharp;
using Microsoft.Extensions.Logging;
using EPC.DataDocs.MockInvestor.Helpers;

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
    /// <summary>
    /// This Class is a wrapper over the Partner API
    /// 1.  CreateResponse
    /// </summary>
    public class PartnerAPIWrapper : IPartnerAPI
    {
        private IOptions<AppSettings> _AppSettings = null;
        private string _ClassName = string.Empty;
        private ILogger _Logger;
        public PartnerAPIWrapper(IOptions<AppSettings> appsettings, ILogger logger)
        {
            _AppSettings = appsettings;
            this._Logger = logger;
            this._ClassName = this.GetType().Name;
            HttpCommunicationHelper.MaxRetryAttempt = _AppSettings.Value.MaxRetryAttempt;
            HttpCommunicationHelper.RetryInterval = _AppSettings.Value.RetryInterval;
        }

        /// <summary>
        /// This method is used to downloan the ZIP file from media server
        /// </summary>
        /// <param name="fileURL"></param>
        /// <returns></returns>
        public RestResponse GetFileContent(string fileURL)
        {
            RestResponse response = null;
            try
            {
                _Logger.LogInformation("[PartnerAPI] - GetFile - Before Get OAuthToken - ", string.Empty, _ClassName);
                // Getting the OAuth Access Token
                var oAuthToken = GetPartnerOAuthToken();

                if (oAuthToken.TokenString != null)
                {
                    _Logger.LogInformation("[PartnerAPI] - GetFile - OAuthToken is not null -  ", string.Empty, _ClassName);
                    // replacing transactionId in the URL

                    var partnerAPIRequestURI = fileURL;
                    _Logger.LogInformation("[PartnerAPI] - GetFile - Before Execute Request", string.Empty, _ClassName);
                    // Executing the Partner API Request                    
                    response = HttpCommunicationHelper.ExecuteDownloadFile(partnerAPIRequestURI, RestSharp.Method.GET);
                }
            }
            catch (Exception ex)
            {
                _Logger.LogInformation("[PartnerAPI] - GetFile - Exception - " + ex.Message, string.Empty, _ClassName);
                _Logger.LogInformation("[PartnerAPI] - GetFile - StackTrace - " + ex.Message, string.Empty, _ClassName);
            }

            return response;
        }

        /// <summary>
        /// This method will do the GetRequest call to the Partner API
        /// </summary>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        public GetRequest GetRequest(string transactionId)
        {
            GetRequest getRequestData = null;

            try
            {
                _Logger.LogInformation("[PartnerAPI] - GetRequest - Before Get OAuthToken - ");

                // Getting the OAuth Access Token
                var oAuthToken = GetPartnerOAuthToken();

                if (oAuthToken.TokenString != null)
                {
                    _Logger.LogInformation("[PartnerAPI] - GetRequest - OAuthToken is not null - ");

                    // replacing transactionId in the URL
                    var partnerAPIRequestURI = _AppSettings.Value.PartnerAPI.RequestURI.Replace("{{transactionId}}", transactionId);
                    var partnerAPIHeader = GetHeaderForPartnerAPI(oAuthToken.TokenString);

                    _Logger.LogInformation("[PartnerAPI] - GetRequest - Before Execute Request - ");

                    // Executing the Partner API Request
                    var response = HttpCommunicationHelper.ExecuteRequest(partnerAPIHeader, _AppSettings.Value.Hosts.Name, partnerAPIRequestURI, RestSharp.Method.GET, "");

                    if (response != null && response.StatusCode == HttpStatusCode.OK)
                    {
                        getRequestData = JsonConvert.DeserializeObject<GetRequest>(response.Content);
                    }

                    _Logger.LogInformation("[PartnerAPI] - GetRequest - Request Data - " + getRequestData);
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError("[PartnerAPI] - GetRequest - Exception - " + ex.Message);
                _Logger.LogError("[PartnerAPI] - GetRequest - StackTrace - " + ex.StackTrace);
            }

            return getRequestData;
        }

        /// <summary>
        /// This method will do a CreateResponse call to the Partner API
        /// </summary>
        /// <param name="responseData"></param>
        /// <param name="transactionId"></param>
        public bool CreateResponse(CreateResponse createResponse, string transactionId)
        {
            var isResponseCreated = false;
            var jsonSerializerSettings = new JsonSerializerSettings();
            jsonSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
            jsonSerializerSettings.NullValueHandling = NullValueHandling.Ignore;

            try
            {

                _Logger.LogInformation("[EPCAdapter] - CreateResponse - Before Get OAuthToken ", string.Empty, _ClassName);
                // Getting the OAuth Access Token
                var oAuthToken = GetPartnerOAuthToken();

                // serialize the CreateResponse object
                var createResponsePayload = JsonConvert.SerializeObject(createResponse, jsonSerializerSettings);


                if (oAuthToken.TokenString != null)
                {
                    _Logger.LogInformation("[EPCAdapter] - CreateResponse - OAuthToken is not null ", string.Empty, _ClassName);
                    // replacing transactionId in the URL
                    var partnerAPIRequestURI = _AppSettings.Value.PartnerAPI.ResponseURI.Replace("{{transactionId}}", transactionId);
                    var partnerAPIHeader = GetHeaderForPartnerAPI(oAuthToken.TokenString);

                    _Logger.LogInformation("[EPCAdapter] - CreateResponse - Before Execute Request ", string.Empty, _ClassName);


                    // Executing the Partner API Request
                    var response = HttpCommunicationHelper.ExecuteRequest(partnerAPIHeader, _AppSettings.Value.Hosts.Name, partnerAPIRequestURI, RestSharp.Method.POST, createResponsePayload);
                    _Logger.LogInformation("[EPCAdapter] - CreateResponse - After ExecuteRequest ", string.Empty, _ClassName);
                    if (response.StatusCode == HttpStatusCode.Created)
                        isResponseCreated = true;
                }
            }
            catch (Exception ex)
            {
                _Logger.LogInformation("[EPCAdapter] - CreateResponse - Exception - " + ex.Message, string.Empty, _ClassName);
                _Logger.LogInformation("[EPCAdapter] - CreateResponse - StackTrace - " + ex.StackTrace, string.Empty, _ClassName);
            }

            return isResponseCreated;
        }

        #region " Private Methods "

        /// <summary>
        /// Gets the Partner OAuth Token required for making Partner API calls
        /// </summary>
        /// <returns></returns>
        private JwtToken GetPartnerOAuthToken()
        {
            JwtToken oAuthToken = null;

            try
            {
                var OAuthKeyDictionary = new Dictionary<string, string>();

                // Gets the headers required for generating the OAuth Access Token
                OAuthKeyDictionary = OAuthWrapper.GetOAuthHeaders(_AppSettings);

                IOAuthWrapper oAuthWrapper = new OAuthWrapper(OAuthKeyDictionary, _AppSettings.Value.Hosts.Name, _AppSettings.Value.Token.OAuthTokenEndPoint);

                // Get the OAuth Access Token
                oAuthToken = oAuthWrapper.GetOAuthAccessToken();
            }
            catch (Exception ex)
            {
                _Logger.LogInformation("[PartnerAPIWrapper] - GetOAuthToken - Exception - " + ex.Message, string.Empty, _ClassName);
                _Logger.LogInformation("[PartnerAPIWrapper] - GetOAuthToken - StackTrace - " + ex.Message, string.Empty, _ClassName);
            }

            return oAuthToken;
        }

        /// <summary>
        /// This function will build and return the header required for Partner API requests
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private Dictionary<string, string> GetHeaderForPartnerAPI(string token)
        {
            var partnerAPIHeader = new Dictionary<string, string>
            {
                { "Authorization", "Bearer " + token },
                { "Content-Type", "application/json" }
            };

            return partnerAPIHeader;
        }

        #endregion
    }
}
