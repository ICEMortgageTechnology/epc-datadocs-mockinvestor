using EPC.DataDocs.MockInvestor.Interfaces.Processor;
using EPC.DataDocs.MockInvestor.Models;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using System.IO;
using EPC.DataDocs.MockInvestor.Wrappers;
using RestSharp;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

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

namespace EPC.DataDocs.MockInvestor.Processor
{
    public class RequestProcessor : IRequestProcessor
    {
        private string _ClassName = string.Empty;

        private ILogger _Logger;
        private PartnerAPIWrapper PartnerAPIWrapper { get; set; }
        private IOptions<AppSettings> _AppSettings { get; set; }
        private WebhookNotificationBody _WebhookBody { get; set; }

        /// <summary>
        /// Order status supported by MockInvestor
        /// </summary>
        private enum OrderStatus
        {
            Delivered,
            Error
        }



        private struct OrderMessage
        {
            public const string DeliveredSuccessMessage = "The loan package has been delivered";
            public const string DeliveredFailedMessage = "Unable to download loan package";
        }

        public RequestProcessor(WebhookNotificationBody _WebhookBody, IOptions<AppSettings> appSettings, ILogger logger)
        {
            this._WebhookBody = _WebhookBody;
            this._ClassName = this.GetType().Name;
            this._Logger = logger;
            this._AppSettings = appSettings;
            this.PartnerAPIWrapper = new PartnerAPIWrapper(this._AppSettings, logger);
        }

        /// <summary>
        /// This method will process the webhook request
        /// </summary>
        public void ProcessWebhookRequest()
        {
            try
            {
                var transactionId = this._WebhookBody.meta != null ? _WebhookBody.meta.resourceId : string.Empty;

                if (!string.IsNullOrEmpty(transactionId))
                {
                    _Logger.LogInformation("POST - api/webhook - Transaction ID - " + transactionId, string.Empty, _ClassName);
                    if (String.Compare(_WebhookBody.eventType, "CreateRequest", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        //Call GetRequest to get loan package from transaction id
                        var requestData = PartnerAPIWrapper.GetRequest(transactionId);
                        _Logger.LogInformation("GetRequest api called.", string.Empty, _ClassName);

                        bool isSuccess = ProcessRequestData(requestData, transactionId);

                        CreateResponse createRespnse = new CreateResponse
                        {
                            orders = new List<Order>()
                        };

                        Order order = SetOrderStatus(requestData, isSuccess);
                        createRespnse.orders.Add(order);

                        if (!PartnerAPIWrapper.CreateResponse(createRespnse, transactionId))
                        {
                            _Logger.LogInformation("CreateResponse is failed", string.Empty, _ClassName);
                        }
                    }
                }
                else
                {
                    _Logger.LogError("Transaction id is empty", string.Empty, _ClassName);
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex.Message, "ProcessWebhookRequest", _ClassName);
            }
        }

        /// <summary>
        /// This method is used to set a order response for post notification
        /// </summary>
        /// <param name="requestData"></param>
        /// <param name="isSuccess"></param>
        /// <returns>Order class</returns>
        private Order SetOrderStatus(GetRequest requestData, bool isSuccess)
        {
            Order order = new Order();
            if (requestData != null && requestData.product != null)
            {
                var investorOrders = requestData.product.options["investorOptions"].ToString();
                JArray arr = JArray.Parse(string.IsNullOrEmpty(investorOrders) ? "[]" : investorOrders);
                if (arr != null && arr.Count > 0)
                {
                    order.id = (string)arr.First.SelectToken("OrderId");
                }
            }
            order.orderDateTime = DateTime.Now.ToString();
            if (isSuccess)
            {
                order.orderStatus = OrderStatus.Delivered.ToString();
                order.message = OrderMessage.DeliveredSuccessMessage;
            }
            else
            {
                order.orderStatus = "Not Delivered";
                order.message = OrderMessage.DeliveredFailedMessage;
            }
            return order;
        }

        /// <summary>
        /// This method is used to download file from media server and copy on partner location
        /// </summary>
        /// <param name="requestData"></param>
        /// <param name="transactionId"></param>
        /// <returns>bool</returns>
        private bool ProcessRequestData(GetRequest requestData, string transactionId)
        {
            try
            {
                if (requestData != null && requestData.product != null)
                {
                    if (requestData.product.attachments != null)
                    {
                        foreach (Attachment attachment in requestData.product.attachments)
                        {
                            string packageURL = attachment.uri;
                            //This method will download package file from API.
                            RestResponse restResponse = PartnerAPIWrapper.GetFileContent(packageURL);

                            if (restResponse != null && restResponse.RawBytes != null)
                            {
                                if (CreateArchiveFile(restResponse, requestData))
                                {
                                    _Logger.LogInformation("File is copied on partner location.", string.Empty, _ClassName);
                                    return true;
                                }
                            }
                            else
                                _Logger.LogInformation("File content is empty.", string.Empty, _ClassName);
                        }
                    }
                    else
                        _Logger.LogInformation("Attachment node is empty", string.Empty, _ClassName);

                }
                else
                    _Logger.LogInformation("Get Request Data is  null", string.Empty, _ClassName);
            }
            catch (Exception ex)
            {
                _Logger.LogError("Exception occured" + ex.Message, string.Empty, _ClassName);
            }
            return false;
        }

        /// <summary>
        /// This method is used to return a ZIP file name as per URLA
        /// </summary>
        /// <param name="restResponse"></param>
        /// <returns></returns>
        private bool CreateArchiveFile(RestResponse restResponse, GetRequest requestData)
        {
            string fileName = string.Empty;
            string fileExtention = string.Empty;
            try
            {
                if (restResponse.ContentType.ToLower().Contains("zip"))
                {
                    using (MemoryStream stream = new MemoryStream(restResponse.RawBytes, true))
                    //using (MemoryStream stream = new MemoryStream())
                    {
                        //stream.Write(restResponse.RawBytes, 0, restResponse.RawBytes.Length);
                        ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Update, true);
                        string ULIName = string.Empty;

                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            if (entry.Name.Contains("_"))
                            {
                                ULIName = entry.Name.Substring(0, entry.Name.IndexOf('_'));
                                fileName = entry.Name.Substring(0, entry.Name.IndexOf('_')) + "_" + DateTime.Now.ToString("yyyyMMddHHmmss");

                                break;
                            }
                        }


                        if (string.IsNullOrEmpty(fileName))
                        {
                            fileName = restResponse.ResponseUri.AbsolutePath.Substring(restResponse.ResponseUri.AbsolutePath.LastIndexOf('/') + 1);
                        }
                        fileExtention = "zip";

                        string archiveLocation = string.Format("{0}\\{1}", _AppSettings.Value.PackageLocation, fileName);

                        archive.ExtractToDirectory(archiveLocation);

                        string companySettingFileName = string.Empty;
                        if (!string.IsNullOrEmpty(ULIName))
                        {
                            companySettingFileName = string.Format("{0}__SubmissionData.txt", ULIName);
                        }
                        else
                        {
                            companySettingFileName = string.Format("{0}__SubmissionData.txt", fileName);
                        }
                        FileDetails fileDetails = new FileDetails
                        {
                            credentials = requestData.credentials
                        };

                        var compnaysettingJSON = JsonConvert.SerializeObject(fileDetails);


                        //var fileInfo = new FileInfo(string.Format(@s"{0}\{1}", ULIName, companySettingFileName);
                        File.WriteAllBytes(string.Format(@"{0}\{1}", archiveLocation, companySettingFileName), Encoding.ASCII.GetBytes(compnaysettingJSON));

                        ZipFile.CreateFromDirectory(archiveLocation, string.Format("{0}.{1}", archiveLocation, fileExtention));

                        DirectoryInfo di = new DirectoryInfo(archiveLocation);
                        foreach (FileInfo fi in di.GetFiles())
                        {
                            fi.Delete();
                        }
                        Directory.Delete(archiveLocation);

                        //File.Delete(archiveLocation);
                        return true;
                    }
                }
                else
                {
                    _Logger.LogError("File extension is not zip", "CreateArchiveFile", _ClassName);
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex.Message, "CreateArchiveFile", _ClassName);
            }
            return false;
        }
    }
}
