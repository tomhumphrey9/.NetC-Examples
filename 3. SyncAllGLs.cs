using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/*
    All private IP and sensative data has been removed. Including cutom Docuworx logging. DocuWare & MYOB Advanced are public APIs.

    Azure Functions microservice called from DocuWare workflow to sync GL codes to index file cabinet

    Parameters:
    FcGUID: DocuWare File Cabinet GUID (destination)
    Offset: how many records to skip
    Count: how many records to sync
    Tennant: optional, only if multiple tenants per DocuWare instance

    Included the old broken code below
*/

namespace MyobAdvancedFunction
{
    public class SyncAllGLs
    {
        CustomPackage customPackage; 
        string dwURL = "";

        public class MyobResponse
        {
            public int? NextID { get; set; }
        }


        [Function("SyncAllGLs")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            customPackage = new CustomPackage();
            string authHeaderVal = "";

            try
            {
                string? body = req.ReadAsString();
                //Write input body to logs
                if (req.Headers.TryGetValues("Authorization", out var authValues))
                {
                    string headerValues = customPackage.someMethod(authValues.FirstOrDefault());
                    dwURL = headerValues.Split(',')[1];
                    authHeaderVal = headerValues.Split(',')[0];

                }    
                else
                {
                    //log: Authorization header not found in request
                    ResponseObj<MyobResponse> obj = customPackage.ReturnFail<MyobResponse>("Authorization header not found in request.");
                    var badResponse = req.CreateResponse(HttpStatusCode.OK);
                    badResponse.WriteAsJsonAsync(obj);
                    return badResponse;
                }
                
                var details = JObject.Parse(body ?? "");
                string fcGUID = "", offset = "", count = "", tenant = "";

                if (details.SelectToken("FcGUID") != null)
                    fcGUID = Convert.ToString(details["FcGUID"]) ?? "";
                if (details.SelectToken("Offset") != null)
                    offset = Convert.ToString(details["Offset"]) ?? "";
                if (details.SelectToken("Count") != null)
                    count = Convert.ToString(details["Count"]) ?? "";
                if (details.SelectToken("Tenant") != null)
                    tenant = Convert.ToString(details["Tenant"]) ?? "";

                if (fcGUID == "" || dwURL == "" || offset == "" || count == "")
                {
                    //log: All required data not found in json!
                    ResponseObj<MyobResponse> obj = customPackage.ReturnFail<MyobResponse>("All required data not found in json!");
                    var response2 = req.CreateResponse(HttpStatusCode.OK);
                    response2.WriteAsJsonAsync(obj);
                    return response2;
                }
                

                //login to DocuWare
                customPackage.login(authHeaderVal, dwURL);
                var responseObject = SyncGLs(fcGUID, Convert.ToInt32(offset), Convert.ToInt32(count), tenant);

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.WriteAsJsonAsync(responseObject);
                return response;
            }
            catch (Exception e)
            {
                //log exception
                ResponseObj<MyobResponse> obj = customPackage.ReturnFail<MyobResponse>(e.ToString());
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.WriteAsJsonAsync(obj);
                return response;
            }
        }
        public ResponseObj<MyobResponse> SyncGLs(string fcGUID, int offset, int count, string tenant)
        {
            int lastprocessId = offset;
            try
            {
                if (customPackage != null)
                {
                    string lookupvalue = Regex.Replace(dwURL ?? "", "[^a-zA-Z0-9%]", string.Empty) + tenant;

                    MYOBConnection myobConnection = new MYOBConnection(lookupvalue, customPackage);
                    
                    var accounts = myobConnection.GetGLs(offset, count);
                    var allAccounts = JsonConvert.DeserializeObject<List<AccountData>>(accounts);

                    if(allAccounts == null) throw new Exception("Error getting GL Codes");

                    if(allAccounts.Count == 0)
                    {
                        //Log success "No more data to be processed"
                        return customPackage.ReturnSuccess("No more data to be processed", new MyobResponse { NextID = 0 });
                    }

                    foreach (var account in allAccounts)
                    {
                        List<Fields> LF = MYOBDataToFields.dataToFields(account);

                        customPackage.uploadDocumentWithIndexFields(fcGUID, LF);

                        lastprocessId++;
                    }

                    //Log: Success "GLs synced successfully" & lastprocessId
                    return customPackage.ReturnSuccess("GLs synced successfully", new MyobResponse { NextID = lastprocessId });

                }
                else
                {
                    //log error "Unable to connect with customPackage class"
                        
                    return customPackage.ReturnFail<MyobResponse>("Unable to connect with customPackage class");

                }
            }
            catch (Exception ex)
            {
                //Log: error exception
                return customPackage.ReturnFail(ex.ToString(), new MyobResponse { NextID = lastprocessId });
            }
        }





        /********************
         *  BELOW is an example of the code before I replaced it with the new methods
         * 
         * This is not an example of my coding ability - rather an example of what I recognised needed a complete rewrite.
         * 
         * Identical code was duplicated accross 9 files doing similar things. e.g. logging in to myob, converting myob response to DocuWare fields etc.
         * 
         * This code did not work. It threw this unhandled exception:
         * 
         * System.IndexOutOfRangeException: Index was outside the bounds of the array. 
         * at MyobAdvancedFunction.SyncAllGLs.SyncGLs(String fcGUID, Int32 offset, Int32 count) 
         * in /home/vsts/work/1/s/MyobAdvancedFunction/SyncAllGLs.cs:line 93
         * 
         */



        public ResponseObj<MyobResponse> SyncGLs(string fcGUID, int offset, int count)
        {
            int lastprocessId = offset;
            try
            {
                if (customPackage != null)
                {
                    string body = "{\r\n  \"name\" : \"x@docuworx.com.au\",\r\n  \"password\" : \"Welcome123\",\r\n  \"tenant\" : \"someTennant\",\r\n  \"branch\" : \"Branch\"\r\n}";

                    string cookie = DoPost(body);
                    var accounts = GetGLs(cookie);
                    var AllAccounts = JsonConvert.DeserializeObject<List<AccountData>>(accounts);
                    for (int i = offset; i < (offset + count); i++)
                    {
                        foreach (var account in AllAccounts)
                        {
                            if (account.rowNumber == i)
                            {
                                PropertyInfo[] infos = account.GetType().GetProperties();
                                Dictionary<string, object> keyValues = new Dictionary<string, object>();
                                foreach (PropertyInfo info in infos)
                                {
                                    keyValues.Add(info.Name + "|" + info.PropertyType.Name, info.GetValue(account, null));
                                }
                                List<Fields> LF = new List<Fields>();
                                foreach (var keyValue in keyValues)
                                {
                                    Fields F = new Fields();
                                    F.FieldName = keyValue.Key.Split('|')[0].ToUpper();
                                    if (keyValue.Key.Split('|')[0] == "id")
                                    {
                                        F.Item = keyValue.Value;
                                        F.ItemElementName = "String";
                                    }
                                    else if (keyValue.Key.Split('|')[0] == "rowNumber")
                                    {
                                        F.Item = keyValue.Value;
                                        F.ItemElementName = "Int";
                                    }
                                    else
                                    {
                                        var vv = keyValue.Value.GetType().GetProperties();
                                        var item = Convert.ToString(vv[0].GetValue(keyValue.Value));
                                        var type = vv[0].PropertyType.Name;

                                        if (type == "String" || type == "Boolean")
                                        {
                                            F.Item = item;
                                            F.ItemElementName = "String";
                                        }
                                        else if (type == "Int32")
                                        {
                                            F.Item = Convert.ToInt32(item);
                                            F.ItemElementName = "Int";
                                        }
                                        else if (type == "DateTime")
                                        {
                                            F.Item = Convert.ToDateTime(item).ToString("yyyy-MM-dd");
                                            F.ItemElementName = "Date";
                                        }
                                        else if (type == "Double")
                                        {
                                            F.Item = Math.Round(Convert.ToDouble(item), 2);
                                            F.ItemElementName = "Decimal";
                                        }
                                    }
                                    LF.Add(F);
                                }

                                Document newdoc = new Document();
                                newdoc.Fields = LF;
                                string jsonData = JsonConvert.SerializeObject(newdoc, Newtonsoft.Json.Formatting.None,
                                                                    new JsonSerializerSettings
                                                                    {
                                                                        NullValueHandling = NullValueHandling.Ignore
                                                                    });
                                string? uploadedDocument = customPackage.uploadDocumentWithIndexFields(fcGUID, jsonData);

                                //var uploadedDoc = JObject.Parse(uploadedDocument);
                                //var fields = uploadedDoc["Fields"];
                                //int uploadedDocId = 0;
                                //foreach (var field in fields)
                                //{
                                //    if (field["FieldName"].ToString() == "DWDOCID")
                                //    {
                                //        uploadedDocId = Convert.ToInt32(field["Item"]);
                                //        break;
                                //    }
                                //}
                                lastprocessId++;
                            }
                        }
                    }
                    if ((offset + count) >= AllAccounts.Count)
                    {
                        
                        return customPackage.ReturnSuccess<MyobResponse>("No more data to be processed");
                    }
                    else
                    {
                        
                        return customPackage.ReturnSuccess("All GLs pushed successfully", new MyobResponse { MyobID = (offset + count - 1) });
                    }
                }
                else
                {
                    
                    return customPackage.ReturnFail<MyobResponse>("Unable to connect with dwrest class");

                }
            }
            catch (Exception ex)
            {
                
                return customPackage.ReturnFail(ex.ToString(), new MyobResponse { MyobID = (lastprocessId - 1) });
            }
        }
        private string DoPost(string body)
        {
            string Url = @"https://x.myobadvanced.com/(W(1))/entity/auth/login";

            RestClient client = new RestClient(new RestClientOptions("https://x.myobadvanced.com")
            {
                MaxTimeout = -1
            });
            RestRequest restRequest = new RestRequest(Url, RestSharp.Method.Post);
            restRequest.AddHeader("Content-Type", "application/json");
            //restRequest.AddHeader("Accept", "application/json");

            restRequest.AddStringBody(body, DataFormat.Json);

            RestResponse restResponse = client.Execute(restRequest);
            var cookie = restResponse.Cookies;
            string token = "";
            for (int i = 0; i < cookie.Count(); i++)
            {
                if (token == "")
                    token = cookie[i].Name + "=" + cookie[i].Value;
                else
                    token = token + ";" + cookie[i].Name + "=" + cookie[i].Value;
            }
            var SCode = (int)restResponse.StatusCode;
            if (restResponse.IsSuccessStatusCode)
            {
                return token;
                //return SCode.ToString() + " " + restResponse.StatusCode.ToString();
            }

            throw new Exception("Returned status code " + restResponse.StatusCode.ToString() + " for request " + restRequest.Resource);
        }

        //get every single GL, and later throw unwanted ones away 
        private string GetGLs(string token)
        {
            string Url = @"https://x.myobadvanced.com/(W(1))/entity/Default/22.200.001/Account";

            RestClient client = new RestClient(new RestClientOptions("https://x.myobadvanced.com")
            {
                MaxTimeout = -1
            });
            RestRequest restRequest = new RestRequest(Url, RestSharp.Method.Get);
            restRequest.AddHeader("Content-Type", "application/json");
            restRequest.AddHeader("Accept", "application/json");
            restRequest.AddHeader("Cookie", token);

            RestResponse restResponse = client.Execute(restRequest);
            if (restResponse.IsSuccessStatusCode)
            {
                return restResponse.Content;
            }

            throw new Exception("Returned status code " + restResponse.StatusCode.ToString() + " for request " + restRequest.Resource);
        }
    }
}


