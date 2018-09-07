using System;
using System.Collections.Generic;
using EPC.DataDocs.MockInvestor.ExtensionMethods;
using EPC.DataDocs.MockInvestor.Helpers;
using EPC.DataDocs.MockInvestor.Models;
using System.Threading.Tasks;
using EPC.DataDocs.MockInvestor.Processor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

namespace EPC.DataDocs.MockInvestor.Controllers
{
    [Produces("application/json")]
    [Route("api/webhook")]
    public class WebhookController : Controller
    {
        private readonly IOptions<AppSettings> _AppSettings;
        private string _ClassName = string.Empty;
        private ILoggerFactory _Factory;
        private ILogger _Logger;

        public WebhookController(IOptions<AppSettings> appSettings)
        {
            _AppSettings = appSettings;
            this._ClassName = this.GetType().Name;
            this._Factory = LogHelper.LoggerFactory;
            this._Logger = this._Factory.CreateLogger("WebHookController");
        }

        // POST api/webhook
        [HttpPost]
        public IActionResult Post([FromBody]JObject data)
        {
            _Logger.LogInformation("POST - api/webhook - [STARTS]", string.Empty, _ClassName);
            // Checking if the request payload is null
            if (data != null)
            {
                _Logger.LogInformation("POST - api/webhook -  Received Webhook Notification at", string.Empty, _ClassName);
                var webhookSecret = _AppSettings.Value.Token.WebhookSecret;
                var requestSignature = Request.Headers["Elli-Signature"].ToString();
                var requestEnvironment = Request.Headers["Elli-Environment"].ToString();

                _Logger.LogInformation("POST - api/webhook - Request Elli-Signature - " + requestSignature, string.Empty, _ClassName);
                _Logger.LogInformation("POST - api/webhook - Request Elli-Environment - " + requestEnvironment, string.Empty, _ClassName);

                // generate the webhook token from the payload and secret using HMACSHA
                var webHookToken = WebHookHelper.GetWebhookNotificationToken(data.ToString(Formatting.None), webhookSecret);

                // Check if the generated WebHook token is similar to the request signature received in the header.
                if (WebHookHelper.IsValidWebhookToken(requestSignature, webHookToken))
                {
                    var webHookBody = new WebhookNotificationBody()
                    {
                        eventId = data.GetValue<string>("eventId"),
                        eventTime = data.GetValue<DateTime>("eventTime"),
                        eventType = data.GetValue<string>("eventType"),
                        meta = data.GetValue<Meta>("meta")
                    };

                    var requestProcessor = new RequestProcessor(webHookBody, _AppSettings,_Logger);

                    // processing the webhook request async.
                    var task = new Task(() => requestProcessor.ProcessWebhookRequest());
                    task.Start();

                }
                else
                    _Logger.LogInformation("POST - api/webhook - WebHook Token is Invalid" , string.Empty, _ClassName);

            }
            _Logger.LogInformation("POST - api/webhook - [ENDS]", string.Empty, _ClassName);

            return Ok();
        }

        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }
    }
}
