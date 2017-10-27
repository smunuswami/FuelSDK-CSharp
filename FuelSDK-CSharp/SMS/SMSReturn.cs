﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

namespace FuelSDK.SMS
{
    internal class SMSReturn
    {

        internal static QueueMOResponse QueueMO(QueueMO obj)
        {
            SMSResponse resp = ExecuteFuel(obj, obj.RequiredURLProperties, "POST", true);
            if (resp.Code == HttpStatusCode.Accepted)
            {
                var result = JsonConvert.DeserializeObject<QueueMOResponse>(resp.Response);
                result.Code = resp.Code;
                return result;
            }
            else
            {
                throw new FuelSDKException(resp.Message);
            }
        }

        internal static string CreateKeyword(SMSKeyword obj)
        {
            SMSResponse resp = ExecuteFuel(obj, obj.RequiredURLProperties, "POST", true);
            if (resp.Code == HttpStatusCode.Accepted)
            {
                var jObj = JObject.Parse(resp.Response);
                return jObj["keywordId"].ToString();
            }
            else
            {
                throw new FuelSDKException(resp.Message);
            }
        }

        internal static string DeleteKeywordByKeywordId(FuelObject obj)
        {
            SMSResponse resp = ExecuteFuel(obj, obj.RequiredURLProperties, "DELETE", false);
            if (resp.Code == HttpStatusCode.Accepted)
            {
                var jObj = JObject.Parse(resp.Response);
                return jObj["status"].ToString();
            }
            else
            {
                throw new FuelSDKException(resp.Message);
            }
        }

        internal ETSMSKeywordResponse PerformKeywordOperation(FuelObject obj, string method)
        {
            SMSResponse resp = ExecuteFuel(obj, obj.RequiredURLProperties, method, true);
            ETSMSKeywordResponse result = new ETSMSKeywordResponse();
            if (!string.IsNullOrEmpty(resp.Response))
            {
                result.Code = resp.Code;
                var x = JObject.Parse(resp.Response);
                if (x["keywordId"] != null)
                {
                    result.KeywordId = x["keywordId"].ToString();
                }
                else if (x["status"] != null)
                {
                    result.Status = x["status"].ToString();
                }

            }
            else 
            {
                result.Code = resp.Code;
                var x = JObject.Parse(resp.Message);
                if (x["errors"] != null)
                {
                    result.Error = x["errors"].ToString();
                }
            }
            return result;
        }

        internal SMSOptInResponse CreateOptInMessage(FuelObject obj, string method)
        {
            SMSResponse resp = ExecuteFuel(obj, obj.RequiredURLProperties, method, true);
            SMSOptInResponse result = new SMSOptInResponse();
            if (!string.IsNullOrEmpty(resp.Response))
            {
                result.Code = resp.Code;
                var x = JObject.Parse(resp.Response);
                if (x["messageID"] != null)
                {
                    result.MessageID = x["messageID"].ToString();
                }

            }
            else
            {
                result.Code = resp.Code;
                var x = JObject.Parse(resp.Message);
                if (x["message"] != null)
                {
                    result.Message = x["message"].ToString();
                }
                if (x["errors"] != null)
                {
                    result.Error = x["errors"].ToString();
                }
                if (x["errorcode"] != null)
                {
                    result.ErrorCode = x["errorcode"].ToString();
                }
                if (x["documentation"] != null)
                {
                    result.Documentation = x["documentation"].ToString();
                }
                if (x["validationErrors"] != null)
                {
                    result.ValidationErrors = x["validationErrors"].ToString();
                }
                if (x["objectErrors"] != null)
                {
                    result.ObjectErrors = x["objectErrors"].ToString();
                }
                if (x["fieldErrors"] != null)
                {
                    result.FieldErrors = x["fieldErrors"].ToString();
                }
            }
            return result;
        }

        internal SMSRefreshListResponse RefreshList(FuelObject obj, string method)
        {
            SMSResponse resp = ExecuteFuel(obj, obj.RequiredURLProperties, method, true);
            SMSRefreshListResponse result = new SMSRefreshListResponse();
            if (!string.IsNullOrEmpty(resp.Response))
            {
                result.Code = resp.Code;
                var x = JObject.Parse(resp.Response);
                if (x["tokenId"] != null)
                {
                    result.TokenId = x["tokenId"].ToString();
                }
                if (x["lastPublishDate"] != null)
                {
                    result.LastPublishDate = x["lastPublishDate"].ToString();
                }
            }
            else
            {
                result.Code = resp.Code;
                var x = JObject.Parse(resp.Message);
                if (x["errors"] != null)
                {
                    result.Error = x["errors"].ToString();
                }
            }

            return result;
        }

        private static SMSResponse ExecuteFuel(FuelObject obj, string[] required, string method, bool postValue)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            obj.AuthStub.RefreshToken();

            object propValue;
            string propValueAsString;
            var completeURL = obj.Endpoint;
            if (required != null)
                foreach (string urlProp in required)
                {
                    var match = false;
                    foreach (var prop in obj.GetType().GetProperties())
                    {
                        if (prop.Name == "UniqueID")
                            continue;
                        if (obj.URLProperties.Contains(prop.Name) && (propValue = prop.GetValue(obj, null)) != null)
                            if ((propValueAsString = propValue.ToString().Trim()).Length > 0 && propValueAsString != "0")
                                match = true;
                    }
                    if (!match)
                        throw new Exception("Unable to process request due to missing required property: " + urlProp);
                }
            foreach (var prop in obj.GetType().GetProperties())
            {
                if (prop.Name == "UniqueID")
                    continue;
                if (obj.URLProperties.Contains(prop.Name) && (propValue = prop.GetValue(obj, null)) != null)
                    if ((propValueAsString = propValue.ToString().Trim()).Length > 0 && propValueAsString != "0")
                        completeURL = completeURL.Replace("{" + prop.Name + "}", propValueAsString);
            }

            // Clean up not required URL parameters
            if (obj.URLProperties != null)
                foreach (string urlProp in obj.URLProperties)
                    completeURL = completeURL.Replace("{" + urlProp + "}", string.Empty);

            completeURL += "?access_token=" + obj.AuthStub.AuthToken;
            if (obj.Page != 0)
                completeURL += "&page=" + obj.Page.ToString();

            var request = (HttpWebRequest)WebRequest.Create(completeURL.Trim());
            request.Method = method;
            request.ContentType = "application/json";
            request.UserAgent = ETClient.SDKVersion;

            if (postValue)
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                    streamWriter.Write(JsonConvert.SerializeObject(obj));

            // Get the response
            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var dataStream = response.GetResponseStream())
                using (var reader = new StreamReader(dataStream))
                {
                    SMSResponse resp = new SMSResponse
                    {
                        Code = response.StatusCode,
                        Message = response.ToString(),
                        Response = reader.ReadToEnd()
                    };
                    return resp;
                }
            }
            catch (WebException we)
            {
                using (var stream = we.Response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    SMSResponse resp = new SMSResponse
                    {
                        Code = ((HttpWebResponse)we.Response).StatusCode,
                        Message = reader.ReadToEnd(),
                        Response = string.Empty
                    };
                    return resp;
                }
            }
        }
    }
}
