using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MyobAdvancedFunction.Vendors;
using System.Text.RegularExpressions;

/*
    All private IP and sensative data has been removed. Including cutom Docuworx logging. DocuWare & MYOB Advanced are public APIs.

    Azure Functions microservice called from DocuWare workflow to sync MYOB Vendors to index file cabinet

    Updates existing records with changes or creates new records.
    If doing full sync use SyncAllVendors it will be quicker.
    
    Parameters:
    FcGUID: DocuWare File Cabinet GUID (destination)
    Offset: how many records to skip
    Count: how many records to sync
    LastModifiedDateTime: Most recent modified date in file cabinet. 
                            MYOB will only return records modified since this datetime.
    Tennant: optional, only if multiple tenants per DocuWare instance

    
*/


namespace MyobAdvancedFunction
{
    public class SyncVendorDeltas
    {
        CustomPackage customPackage;
        string dwURL = "";

        public class MyobResponse
        {
            public int? NextID { get; set; }
        }


        [Function("SyncVendorsDeltas")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            customPackage = new CustomPackage();
            string authHeaderVal = "";
            try
            {
                string? body = req.ReadAsString();
                //Log Info: input body
                if (req.Headers.TryGetValues("Authorization", out var authValues))
                {
                    string headerValues = customPackage.someMethod(authValues.FirstOrDefault());
                    dwURL = headerValues.Split(',')[1];
                    authHeaderVal = headerValues.Split(',')[0];
                }
                else
                {
                    //Log Error: "Authorization header not found in request."
                    ResponseObj<MyobResponse> obj = customPackage.ReturnFail<MyobResponse>("Authorization header not found in request.");
                    var badResponse = req.CreateResponse(HttpStatusCode.OK);
                    badResponse.WriteAsJsonAsync(obj);
                    return badResponse;
                }
                
                var details = JObject.Parse(body ?? "");
                string fcGUID = "", lastModified = "", offset = "", count = "", tenant = "";

                if (details.SelectToken("FcGUID") != null)
                    fcGUID = Convert.ToString(details["FcGUID"]) ?? "";
                if (details.SelectToken("LastModifiedDateTime") != null)
                    lastModified = Convert.ToString(details["LastModifiedDateTime"]) ?? "";
                if (details.SelectToken("Offset") != null)
                    offset = Convert.ToString(details["Offset"]) ?? "";
                if (details.SelectToken("Count") != null)
                    count = Convert.ToString(details["Count"]) ?? "";
                if (details.SelectToken("Tenant") != null)
                    tenant = Convert.ToString(details["Tenant"]) ?? "";

                DateTime parsedDate;
                if (DateTime.TryParse(lastModified, out parsedDate))
                {
                    // Format the DateTime object back to a string in the ISO 8601 format
                    lastModified = parsedDate.ToString("yyyy-MM-ddTHH:mm:ss");

                }


                if (fcGUID == "" || dwURL == "" || lastModified == "" || offset == "" || count == "")
                {
                    //log: All required data not found in json!
                    ResponseObj<MyobResponse> obj = customPackage.ReturnFail<MyobResponse>("All required data not found in json!");
                    var response2 = req.CreateResponse(HttpStatusCode.OK);
                    response2.WriteAsJsonAsync(obj);
                    return response2;
                }
                

                //login to DocuWare
                customPackage.login(authHeaderVal, dwURL);

                var responseObject = VendorsUsingTimeframe(fcGUID, lastModified, Convert.ToInt32(offset), Convert.ToInt32(count), tenant);
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.WriteAsJsonAsync(responseObject);
                return response;

            }
            catch (Exception e)
            {
                //Log Error: Exception
                ResponseObj<MyobResponse> obj = customPackage.ReturnFail<MyobResponse>(e.ToString());
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.WriteAsJsonAsync(obj);
                return response;
            }
        }


        public ResponseObj<MyobResponse> VendorsUsingTimeframe(string fcGUID, string lastModified, int offset, int count, string tenant)
        {
            int lastprocessId = offset;
            try
            {
                string lookupvalue = Regex.Replace(dwURL ?? "", "[^a-zA-Z0-9%]", string.Empty) + tenant;

                MYOBConnection myobConnection = new MYOBConnection(lookupvalue, customPackage);

                var vendors = myobConnection.GetVendors(offset, count, lastModified);
                var allVendors = JsonConvert.DeserializeObject<List<VendorData>>(vendors);

                if (allVendors == null) throw new Exception("Error getting vendors");

                if(allVendors.Count == 0)
                {
                    //Log Success: "No more data to be processed"
                    return customPackage.ReturnSuccess("No more data to be processed", new MyobResponse { NextID = 0 });
                }

                foreach (var vendor in allVendors)
                {
                    List<Fields> LF = MYOBDataToFields.dataToFields(vendor);

                    string? vendorID = vendor.GetType().GetProperty("id")?.GetValue(vendor)?.ToString();
                    if (!string.IsNullOrEmpty(vendorID))
                    {
                        //check to see if the Vendor exists in DocuWare
                        List<int> lookup = customPackage.lookupDocIDsWithIndexSearch(fcGUID, "ID", vendorID, 1);

                        if (lookup.Count > 0)
                        {
                            //Vendor already exists in DW - update it
                            customPackage.updateDocumentIndexFields(fcGUID, LF, lookup[0]);
                        }
                        else
                        {
                            //new vendor - create new record
                            customPackage.uploadDocumentWithIndexFields(fcGUID, LF);
                        }
                                                               
                    }
                    else
                    {
                        //new vendor - create new record
                        customPackage.uploadDocumentWithIndexFields(fcGUID, LF);
                    }
                    
                    lastprocessId++;

                }

                //Log Success: "Vendors synced successfully" & lastprocessId
                return customPackage.ReturnSuccess("Vendors synced successfully", new MyobResponse { NextID = lastprocessId });

            }
            catch (Exception ex)
            {
                //Log Error: Exception
                return customPackage.ReturnFail(ex.ToString(), new MyobResponse { NextID = lastprocessId });
            }
        }
    }


}
