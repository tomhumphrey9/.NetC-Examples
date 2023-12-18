using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Newtonsoft.Json;
using RestSharp;
using System.Data.SqlClient;
using System.Net;

/*
    All private IP and sensative data has been removed. Including cutom Docuworx logging. DocuWare & MYOB Advanced are public APIs.

    Class for connecting to a MYOB advanced cloud instance. Retrieve Vendors & GL Codes. Push in invoice data.
    Connection data & URLs retrieved from Azure SQL table. Passwords stored in Azure KeyVault.

    Changes:
    1. Centralized all connection into this file. Removed 9 copy pasted login method from other files.
    2. Store MYOB cookie in KeyVault - don't have to refresh every time
    3. Retry on failure
    4. Error handling
*/

namespace MyobAdvancedFunction
{
    public class MYOBConnection
    {
        string lookupValue = "";
        CustomPackage customPackage;    //Custom Nuget package I implemented to group common methods & stop code duplication accross projects
        public string Name { get; set; } = "";
        public string Tenant { get; set; } = "";
        public string Branch { get; set; } = "";
        public string VendorURL { get; set; } = "";
        public string GLURL { get; set; } = "";
        public string InvoiceURL { get; set; } = "";
        public string LoginURL { get; set; } = "";
        public string EmployeeURL { get; set; } = "";
        RestClient restClient;
        SecretClient keyVault;
        public KeyVaultSecret myobCookie;
                
        public MYOBConnection(string _lookupValue, CustomPackage _customPackage)
        {
            lookupValue = _lookupValue;
            customPackage = _customPackage;
            restClient = new RestClient(new RestClientOptions
            {
                MaxTimeout = 20000 // 20 seconds in milliseconds
            });

            keyVault = new SecretClient(new Uri($"https://x.vault.azure.net/"), new DefaultAzureCredential());
            KeyVaultSecret sqlPassword = keyVault.GetSecret("Secret");
            string connectionString = string.Format("", sqlPassword.Value);

            string sql = "SELECT 1, 2, 3, 4, 5, 6, 7, 8 FROM TABLE WHERE lookupValue = @lookupValue";
            getDatafromDB(sql);
            
            myobCookie = getMYOBCookie();

            void getDatafromDB(string sql)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        SqlCommand command = new SqlCommand(sql, connection);
                        command.Parameters.AddWithValue("@lookupValue", lookupValue);


                        connection.Open();
                        SqlDataReader reader = command.ExecuteReader();

                        if (reader.Read())
                        {
                            try
                            {
                                // Explicitly casting to string. This will throw an InvalidCastException 
                                // if the database field is DBNull or not a string.
                                Name = (string)reader["1"];
                                Tenant = (string)reader["2"];
                                Branch = (string)reader["3"];
                                LoginURL = (string)reader["4"];

                                // Check for null or empty values
                                if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Tenant) ||
                                    string.IsNullOrEmpty(Branch) || string.IsNullOrEmpty(LoginURL))
                                {
                                    throw new Exception();
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("One or more required fields (Name, Tenant, Branch, LoginURL) are null or empty.", ex);
                            }

                            // Non required fields
                            VendorURL = reader["5"] as string ?? string.Empty;
                            GLURL = reader["6"] as string ?? string.Empty;
                            InvoiceURL = reader["7"] as string ?? string.Empty;
                            EmployeeURL = reader["8"] as string ?? string.Empty;

                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Failure to get MYOB connection data with Lookup: " + lookupValue, ex);
                }
                    
            }

            KeyVaultSecret getMYOBCookie()
            {
                string keyVaultLookupCookie = lookupValue + "COOKIE";
                KeyVaultSecret myobCookie = keyVault.GetSecret(keyVaultLookupCookie);

                if (!customPackage.CheckSecretAgeValid(myobCookie, 1))
                {
                    //cookie older than 1 day - refresh
                    myobCookie = RefreshMYOBCookie();
                }
                return myobCookie;
            }
        }

        private KeyVaultSecret RefreshMYOBCookie()
        {
            try
            {
                string keyVaultLookupPassword = lookupValue  + "PASSWORD";
                string keyVaultLookupCookie = lookupValue  + "COOKIE";
                KeyVaultSecret myobPassword = keyVault.GetSecret(keyVaultLookupPassword);

                var requestBody = new
                {
                    name = Name,
                    password = myobPassword.Value,
                    tenant = Tenant,
                    branch = Branch
                };
                string jsonRequestBody = JsonConvert.SerializeObject(requestBody);

                RestRequest restRequest = new RestRequest(LoginURL, RestSharp.Method.Post);
                restRequest.AddHeader("Content-Type", "application/json");

                restRequest.AddStringBody(jsonRequestBody, DataFormat.Json);

                RestResponse restResponse = restClient.Execute(restRequest);
                
                var cookie = restResponse.Cookies;
                string token = "";
                if (cookie != null)
                {
                    for (int i = 0; i < cookie.Count; i++)
                    {
                        if (token == "")
                            token = cookie[i].Name + "=" + cookie[i].Value;
                        else
                            token = token + ";" + cookie[i].Name + "=" + cookie[i].Value;
                    }

                    if (restResponse.IsSuccessStatusCode && token != "")
                    {
                        //Logged that the MYOB cookie for this client was refreshed

                        return keyVault.SetSecret(keyVaultLookupCookie, token);
                    }
                }
                throw new Exception("Message: Responce: Status Code: " + restResponse.StatusCode.ToString() + " Headrs: " + token + " Content: " + restResponse.Content + " for request " + restRequest.Resource);
            }
            catch (Exception ex)
            {
                throw new Exception("Error Refreshing MYOB Cookie", ex);
            }

        }

        private RestResponse ExecuteRequestAndHandleResponse(RestRequest request, int retryCount = 0)
        {
            const int MaxRetries = 2; // Maximum number of retries

            RestResponse response = restClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return response;
            }
            else
            {
                string errorMessage = $"MYOB returned status code {response.StatusCode} for request {request.Resource}";

                if (!string.IsNullOrEmpty(response.Content))
                {
                    errorMessage += $"\nResponse Content: {response.Content}";
                }

                if (response.ErrorException != null)
                {
                    errorMessage += $"\nError Message: {response.ErrorException.Message}";
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (retryCount < MaxRetries)
                    {
                        //logged error message as warning here

                        myobCookie = RefreshMYOBCookie(); // Refresh the cookie

                        //remove old cookie
                        var cookieParam = request.Parameters.FirstOrDefault(p => p.Type == ParameterType.HttpHeader && p.Name == "Cookie");
                        if (cookieParam != null)
                        {
                            request.Parameters.RemoveParameter(cookieParam);
                        }

                        request.AddHeader("Cookie", myobCookie.Value); // Add the updated cookie


                        return ExecuteRequestAndHandleResponse(request, retryCount + 1); // Recursive call with incremented retry count
                    }
                }

                //if still failing after retries
                throw new Exception(errorMessage);
            }
        }

        //get all vendors
        public string GetVendors(int offset, int count)
        {
            if (VendorURL == null || VendorURL == "") { throw new Exception("VendorURL is not defined."); }

            string topQuery = "$top=" + count;
            string skipQuery = "$skip=" + offset;

            string Url = $"{VendorURL}?{topQuery}&{skipQuery}";

            RestRequest restRequest = new RestRequest(Url, RestSharp.Method.Get);
            restRequest.AddHeader("Content-Type", "application/json");
            restRequest.AddHeader("Accept", "application/json");
            restRequest.AddHeader("Cookie", myobCookie.Value);

            RestResponse response = ExecuteRequestAndHandleResponse(restRequest);
            if (response.Content == null)
            {
                throw new InvalidOperationException("Get Vendors Response content is null.");
            }
            return response.Content;
        }

        //get all vendors modified since a certain date
        public string GetVendors(int offset, int count, string lastModified)
        {
            if (VendorURL == null || VendorURL == "") { throw new Exception("VendorURL is not defined."); }

            string filterQuery = "$filter=LastModifiedDateTime gt datetimeoffset'" + lastModified + "'";
            string topQuery = "$top=" + count;
            string skipQuery = "$skip=" + offset;

            string Url = $"{VendorURL}?{filterQuery}&{topQuery}&{skipQuery}";


            RestRequest restRequest = new RestRequest(Url, RestSharp.Method.Get);
            restRequest.AddHeader("Content-Type", "application/json");
            restRequest.AddHeader("Accept", "application/json");
            restRequest.AddHeader("Cookie", myobCookie.Value);

            RestResponse response = ExecuteRequestAndHandleResponse(restRequest);
            if (response.Content == null)
            {
                throw new InvalidOperationException("Get Vendors Response content is null.");
            }
            return response.Content;
        }

        public string PushInvoice(string invBody)
        {
            if (InvoiceURL == null || InvoiceURL == "") { throw new Exception("InvoiceURL is not defined."); }

            RestRequest restRequest = new RestRequest(InvoiceURL, Method.Put);
            restRequest.AddHeader("Content-Type", "application/json");
            restRequest.AddHeader("Accept", "application/json");
            restRequest.AddHeader("Cookie", myobCookie.Value);
            restRequest.AddStringBody(invBody, DataFormat.Json);

            RestResponse response = ExecuteRequestAndHandleResponse(restRequest);
            if (response.Content == null)
            {
                throw new InvalidOperationException("Push Invoice Response content is null.");
            }
            return response.Content;
        }

        public string GetGLs(int offset, int count)
        {
            if (GLURL == null || GLURL == "") { throw new Exception("GLURL is not defined."); }

            string topQuery = "$top=" + count;
            string skipQuery = "$skip=" + offset;
            string Url = $"{GLURL}?{topQuery}&{skipQuery}";

            RestRequest restRequest = new RestRequest(Url, RestSharp.Method.Get);
            restRequest.AddHeader("Content-Type", "application/json");
            restRequest.AddHeader("Accept", "application/json");
            restRequest.AddHeader("Cookie", myobCookie.Value);

            RestResponse response = ExecuteRequestAndHandleResponse(restRequest);
            if (response.Content == null)
            {
                throw new InvalidOperationException("Get GLs Response content is null.");
            }
            return response.Content;
        }

        public string GetGLs(int offset, int count, string lastModified)
        {
            if (GLURL == null || GLURL == "") { throw new Exception("GLURL is not defined."); }

            string filterQuery = "$filter=LastModifiedDateTime gt datetimeoffset'" + lastModified + "'";
            string topQuery = "$top=" + count;
            string skipQuery = "$skip=" + offset;

            string Url = $"{GLURL}?{filterQuery}&{topQuery}&{skipQuery}";

            RestRequest restRequest = new RestRequest(Url, RestSharp.Method.Get);
            restRequest.AddHeader("Content-Type", "application/json");
            restRequest.AddHeader("Accept", "application/json");
            restRequest.AddHeader("Cookie", myobCookie.Value);

            RestResponse response = ExecuteRequestAndHandleResponse(restRequest);
            if (response.Content == null)
            {
                throw new InvalidOperationException("Get GLs Response content is null.");
            }
            return response.Content;
        }
    }
}
