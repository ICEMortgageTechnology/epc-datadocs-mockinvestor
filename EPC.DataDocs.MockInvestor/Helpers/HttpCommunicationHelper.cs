using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RestSharp;

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

namespace EPC.DataDocs.MockInvestor.Helpers
{
    /// <summary>
    /// This class is a Helper class for all the http communications done by the Reference Integration
    /// It is using RestSharp plugin to make REST calls
    /// </summary>
    public static class HttpCommunicationHelper
    {
        private static ILoggerFactory _Factory;
        private static ILogger _Logger;

        #region Properties
        public static int MaxRetryAttempt { get; set; }
        public static int RetryInterval { get; set; }
        #endregion

        /// <summary>
        /// static constructor
        /// </summary>
        static HttpCommunicationHelper()
        {
            _Factory = LogHelper.LoggerFactory;
            _Logger = _Factory.CreateLogger("HttpCommunicationHelper");
        }

        /// <summary>
        /// Execute POST using RestSharp
        /// </summary>
        /// <param name="requestParameters">Headers required for the REST call</param>
        /// <param name="baseURL">The base URL of the REST API</param>
        /// <param name="uri">The URI of the REST API</param>
        /// <param name="requestType">RequestType here is the REST Verbs (GET, PUT, PATCH, POST, DELETE)</param>
        /// <returns></returns>
        public static string ExecuteRequest(IEnumerable<KeyValuePair<string, string>> requestParameters, string baseURL, string uri, Method requestType)
        {
            var responseString = string.Empty;
            var restClient = new RestClient(baseURL);
            var request = new RestRequest(uri, requestType);
            var response = new RestResponse();

            _Logger.LogInformation("[HttpCommunicationHelper] In Execute Request Method");
            _Logger.LogInformation("[HttpCommunicationHelper] Request Type is " + requestType.ToString());
            _Logger.LogInformation("[HttpCommunicationHelper] Request Base URL is " + baseURL);
            _Logger.LogInformation("[HttpCommunicationHelper] Request URI is " + uri);

            // add request headers if its not null
            if (requestParameters != null)
            {
                _Logger.LogInformation("[HttpCommunicationHelper] requestParameters are not null.");

                foreach (var item in requestParameters)
                {
                    request.AddParameter(item.Key, item.Value);
                    _Logger.LogInformation("[HttpCommunicationHelper] requestParameter - Key - " + item.Key + " | Value - " + item.Value);
                }
            }

            // this will run the task asynchronously
            Task.Run(async () =>
            {
                response = await GetResponseContentAsync(restClient, request) as RestResponse;
            }).Wait();

            // add the response string if the status code is ok
            if (response.StatusCode == HttpStatusCode.OK)
            {
                responseString = response.Content;
            }
            _Logger.LogInformation(response.Content);

            _Logger.LogInformation("[HttpCommunicationHelper] Response URI - " + response.ResponseUri);
            _Logger.LogInformation("[HttpCommunicationHelper] Response Content Type - " + response.ContentType);
            _Logger.LogInformation("[HttpCommunicationHelper] Response Status - " + response.ResponseStatus);
            _Logger.LogInformation("[HttpCommunicationHelper] Response Status Code - " + response.StatusCode.ToString());
            _Logger.LogInformation("[HttpCommunicationHelper] Response Status Description - " + response.StatusDescription);

            return responseString;
        }

        /// <summary>
        /// Execute Request
        /// </summary>
        /// <param name="requestParameters"></param>
        /// <param name="baseURL"></param>
        /// <param name="uri"></param>
        /// <param name="requestType"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public static RestResponse ExecuteRequest(IEnumerable<KeyValuePair<string, string>> requestParameters, string baseURL, string uri, Method requestType, string body = "")
        {
            RestResponse restResponse = null;
            var restClient = new RestClient(baseURL);
            var request = new RestRequest(uri, requestType);
            var response = new RestResponse();

            _Logger.LogInformation("[HttpCommunicationHelper] In Execute Request Method");
            _Logger.LogInformation("[HttpCommunicationHelper] Request Type is " + requestType.ToString());
            _Logger.LogInformation("[HttpCommunicationHelper] Request Base URL is " + baseURL);
            _Logger.LogInformation("[HttpCommunicationHelper] Request URI is " + uri);

            // add body if it is not empty
            if (!string.IsNullOrEmpty(body))
            {
                _Logger.LogInformation("[HttpCommunicationHelper] Body is not null");

                var postdata = SimpleJson.DeserializeObject(body);
                request.AddJsonBody(postdata);

                _Logger.LogInformation("[HttpCommunicationHelper] Request Body - " + body);
            }

            // add request headers if its not null
            if (requestParameters != null)
            {
                _Logger.LogInformation("[HttpCommunicationHelper] requestParameters are not null.");

                foreach (var item in requestParameters)
                {
                    request.AddParameter(item.Key, item.Value, ParameterType.HttpHeader);
                    _Logger.LogInformation("[HttpCommunicationHelper] requestParameter - Key - " + item.Key + " | Value - " + item.Value);
                }
            }

            Task.Run(async () =>
            {
                response = await GetResponseContentAsync(restClient, request) as RestResponse;
            }).Wait();

            restResponse = response;

            _Logger.LogInformation("[HttpCommunicationHelper] Response URI - " + response.ResponseUri);
            _Logger.LogInformation("[HttpCommunicationHelper] Response Content Type - " + response.ContentType);
            _Logger.LogInformation("[HttpCommunicationHelper] Response Status - " + response.ResponseStatus);
            _Logger.LogInformation("[HttpCommunicationHelper] Response Status Code - " + response.StatusCode.ToString());
            _Logger.LogInformation("[HttpCommunicationHelper] Response Status Description - " + response.StatusDescription);

            return restResponse;
        }


        /// <summary>
        /// This method will take file url list to download the content
        /// </summary>      
        /// <param name="downloadFileURL"></param>
        /// <param name="requestType"></param>       
        /// <returns></returns>
        public static RestResponse ExecuteDownloadFile(string downloadFileURL, Method requestType)
        {
            RestResponse restResponse = null;
            var restClient = new RestClient(downloadFileURL);
            var request = new RestRequest(requestType);
            var response = new RestResponse();

            _Logger.LogInformation("[HttpCommunicationHelper] In ExecuteDownloadFileURL Method");
            _Logger.LogInformation("[HttpCommunicationHelper] Request Type is " + requestType.ToString());
            _Logger.LogInformation("[HttpCommunicationHelper] Request Base URL is " + downloadFileURL);

            request.AlwaysMultipartFormData = true;

            Task.Run(async () =>
            {
                response = await GetResponseContentAsync(restClient, request) as RestResponse;
            }).Wait();


            restResponse = response;

            _Logger.LogInformation("[HttpCommunicationHelper] Response URI - " + response.ResponseUri);
            _Logger.LogInformation("[HttpCommunicationHelper] Response Content Type - " + response.ContentType);
            _Logger.LogInformation("[HttpCommunicationHelper] Response Status - " + response.ResponseStatus);
            _Logger.LogInformation("[HttpCommunicationHelper] Response Status Code - " + response.StatusCode.ToString());
            if (response.StatusDescription.ToLower().Contains("error"))
                _Logger.LogError("[HttpCommunicationHelper] Response Status Description - " + response.StatusDescription);
            else
                _Logger.LogInformation("[HttpCommunicationHelper] Response Status Description - " + response.StatusDescription);
            //_Logger.Information("[HttpCommunicationHelper] Response Content - " + response.Content);

            return restResponse;
        }
        /// <summary>
        /// This method will get execute the request and get the response asynchronously
        /// </summary>
        /// <param name="theClient"></param>
        /// <param name="theRequest"></param>
        /// <returns></returns>
        public static Task<IRestResponse> GetResponseContentAsync(RestClient theClient, RestRequest theRequest)
        {
            Task<IRestResponse> restResponse = null;
            int maxRetryCount = MaxRetryAttempt;
            do
            {
                --maxRetryCount;
                var tcs = new TaskCompletionSource<IRestResponse>();
                theClient.ExecuteAsync(theRequest, response =>
                {
                    tcs.SetResult(response);
                });

                _Logger.LogInformation("Status code for " + theRequest.Resource + "  is " + tcs.Task.Result.StatusCode.ToString());
                restResponse = tcs.Task;
                if (restResponse.Result.StatusCode == HttpStatusCode.OK || restResponse.Result.StatusCode == HttpStatusCode.Created)
                {
                    break;
                }
                System.Threading.Thread.Sleep(RetryInterval);

            } while (maxRetryCount > 0);

            return restResponse;
        }
    }
}
