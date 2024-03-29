﻿using inRiver.Remoting;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Objects;
//using ResourceImport;
//using ServerExtension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

// Error layer of inRiver logging will contain Status Codes and generic reasons from the server. 
// Debug layer will contain the request message or the stack trace

namespace Aprimo.InRiver.InboundExtension
{
    public class DataAPI: IInboundDataExtension
    {
        public inRiverContext Context { get; set; }

        // Aprimo access tokens last 10 minutes
        private Stopwatch stopWatch = new Stopwatch();
        private string accessToken = null;
        // DefaultSettings is automatically detected by the inRiver client when the connector is uploaded
        public Dictionary<string, string> DefaultSettings => new Dictionary<string, string>
        {
                { "clientID", "[ClientID]" },
                { "clientSecret", "[ClientSecret]" },
                { "integrationUsername", "[IntegrationUsername]" },
                { "aprimoTenant", "[AprimoTenantName]" },
                { "entityTypeDAMFieldID", "[AprimoFieldIDForEntityType]" }, //inRiverEntityType
                { "entityIDDAMFieldID", "[AprimoFieldIDForEntityID]" }, // inRiverEntityID
                { "resourceIDDAMFieldID", "[AprimoFieldIDForResourceID]" },
                { "statusDAMFieldID",  "[AprimoFieldIDForStatusMessages]"},
                { "ResourceTitle", "[AprimoFieldToUseForInRiverResourceTitle]" },
                { "aprimoRecordIdFieldTypeID", "[inRiverFieldNameToStoreAprimoRecordID]" },
                { "aprimoResourceFromAprimoFieldTypeID", "[inRiverFieldNameForFieldToMarkAResourceFromAprimo]" },
                { "aprimoMetadataForResourceMapping", "AprimoFieldName:InRiverFieldName" },
                { "inRiverEntityUniqueFieldsForIdentifying", "[OptionsListMapping]" } //EX: Product:ProductId;Item:ItemNumber;Channel:ChannelName
        };
        // These dictionaries will be initialized after we get the Context
        private Dictionary<string, string> uniqueFieldDictionary = null;
        private Dictionary<string, string> aprimoMetadataFieldsToMapToInRiverResource = null;
        public DataAPI()
        {
        }

        // Used for testing. inRiver will use a parameterless constructor when it initializes extensions
        public DataAPI(inRiverContext _context)
        {
            Context = _context;

            
            initializeDictionaries();
        }

        #region API Calls
        public string Add(string value)
        {
            // Called by Aprimo DAM when sending an asset to inRiver
            Context.Log(inRiver.Remoting.Log.LogLevel.Information, $"[AprimoTenant] - Add() passed value: {value}");
            // Check if dictionaries are initialized
            initializeDictionaries();
            
            
            dynamic jsonObj = JsonConvert.DeserializeObject(value);
            string val = jsonObj["value"];
            string[] splitInput = val.Split(';');

            string recordID = splitInput[0]; //recordId
            string entityID = splitInput[1]; // The ID of the specific entity 
            string entityType = splitInput[2]; // The entity (Product, Item, etc)
            string operationType = splitInput[3]; // The operation. CREATE, UPDATE, DELETE, etc

            // All requests from the DAM are POST so type of operation will need to be identified in this function
            string statusMessage = "Complete";
            try
            {
                if (operationType == "CREATE")
                {
                    createOperation(recordID, entityType, entityID);
                    
                }
                else if (operationType == "UPDATE")
                {
                    updateOperation(recordID, entityType, entityID);
                }
            }
            catch (ArgumentException e)
            {
                // statusMessage will be used to update Aprimo
                statusMessage = e.Message;
                // Log the error in inRiver
                Context.Log(inRiver.Remoting.Log.LogLevel.Error, $"Aprimo Environment- ArgumentException: {e.Message} ");
            }
            catch (HttpRequestException e)
            {
                // statusMessage will be used to update Aprimo
                statusMessage = e.Message;
                // Log the error in inRiver
                Context.Log(inRiver.Remoting.Log.LogLevel.Error, $"Aprimo Environment- HttpRequestException: {e.Message} ");
            }
            catch (TimeoutException e)
            {
                // statusMessage will be used to update Aprimo
                statusMessage = e.Message;
                // Log the error in inRiver
                Context.Log(inRiver.Remoting.Log.LogLevel.Error, $"Aprimo Environment- TimeoutError: {e.Message} ");
            }
            catch (Exception e)
            {
                // statusMessage will be used to update Aprimo
                statusMessage = e.Message;
                // Log the error in inRiver
                Context.Log(inRiver.Remoting.Log.LogLevel.Error, $"Aprimo Environment- Base Exception: {e.Message} ");
            }
            finally
            {
                // Update Aprimo inRiver Status field with the error/success message.
                string requestBody = @"{" +
                                  @"     ""fields"":  " +
                                  @"       {" +
                                  @"          ""addOrUpdate"":[" +
                                  @"            {" +
                                  @"                ""id"": """ + Context.Settings["statusDAMFieldID"] + @""",     " +
                                  @"                ""localizedValues"":[" +
                                  @"                    {  " +
                                  @"                        ""languageId"":""00000000000000000000000000000000"", " +
                                  @"                        ""value"": """ + statusMessage + @""" " +
                                  @"                    }  " +
                                  @"                  ]  " +
                                  @"            }" +
                                  @"           ]" +
                                  @"       }" +
                                  @"}";

                EditAprimoRecord(requestBody, recordID);
            }
            

            return $"Aprimo Environment- Message from Add()";
        }

        public string Delete(string value)
        {
            throw new NotImplementedException();
        }

        public string Test()
        {
            Context.Log(inRiver.Remoting.Log.LogLevel.Debug, Context.Username);
            return "Aprimo Environment- Testing Data Extension";
        }

        public string Update(string value)
        {
            throw new NotImplementedException();
        }

        #endregion
        #region Helper Functions
        private void createOperation(string recordID, string entityType, string entityID)
        {
            #region 1. Access Token
            // Get access token here instead of in Communicate with Aprimo region. If the validation fails then we try to update Aprimo
        
            accessToken = GetDAMAccessToken();
            #endregion
            #region 2. Validate

            // Check if resource already exists
            Entity resource = Context.ExtensionManager.DataService.GetEntityByUniqueValue(Context.Settings["aprimoRecordIdFieldTypeID"], recordID, LoadLevel.DataAndLinks);
            if (resource != null)
            {
                throw new ArgumentException($"Aprimo Environment- Aprimo record {recordID} already exists as a resource in inRiver"); // Caught in Add()
                
            }

            
            // Check if link already exists
            if(resource != null)
            {
                List<Link> inboundLinks = resource.InboundLinks;
                inboundLinks.ForEach(delegate (Link item)
                {
                    // If the link's source entity is the same one we are trying to link to, throw an error
                    Entity sourceEntity = Context.ExtensionManager.DataService.GetEntity(item.Source.Id, LoadLevel.DataAndLinks);
                    if (sourceEntity.EntityType.ToString() == entityType && (string)sourceEntity.GetField(uniqueFieldDictionary[sourceEntity.EntityType.ToString()]).Data == entityID)
                    {
                        // Trying to link to an entity the resource is already linked to
                        throw new ArgumentException($"Aprimo Environment- Resource with Aprimo record id {recordID} is already linked to a(n) {entityType} with id {entityID}"); // Caught in Add()
                    }
                });
            }
           

            #endregion

            // Get Record from /recordID. The record body contains the masterfilename and the record's metadata fields
            // Get preview download uri from recordID/image/preview
            // Download asset preview
            dynamic recordBody = null;
            string masterFileName = null;
            string resourceTitle = null;
            string previewURI = null;
            string mimeType = null;

            #region 3. Communicate With Aprimo DAM

            

            // Get record's metadata, masterfilelatestversion, and the preview of the masterfile
            recordBody = GetRecord(recordID);
            masterFileName = recordBody["masterFileLatestVersion"]["fileName"];
            previewURI = recordBody["preview"]["uri"];
            // mimeType may come out as octet-stream regularly. You may need to add a file extension to mime type mapping
            mimeType = recordBody["masterFileLatestVersion"]["fileType"]["mimeType"];

            foreach (dynamic obj in recordBody["fields"]["items"])
            {
                if (obj.fieldName == Context.Settings["ResourceTitle"])
                {
                    resourceTitle = obj.localizedValues[0].value;
                    break;
                }

            }
            #endregion
           
            #region 4. Resource Creation
            Entity newResource = null;

            
            newResource = CreateResource(masterFileName, previewURI, resourceTitle, recordID, mimeType);
            
            #endregion

            #region 5. Linking
            Link resourceLink = null;

          
           
            resourceLink = CreateLink(newResource, entityType, entityID);
           
            #endregion

            #region 6. Edit Aprimo Record
            int resourceId = newResource.Id;
            string requestBody =  @"{" +
                                  @"     ""fields"":  " +
                                  @"       {" +
                                  @"          ""addOrUpdate"":[" +
                                  @"            {" +
                                  @"                ""id"": """ + Context.Settings["resourceIDDAMFieldID"] + @""",     " +
                                  @"                ""localizedValues"":[" +
                                  @"                    {  " +
                                  @"                        ""languageId"":""00000000000000000000000000000000"", " +
                                  @"                        ""value"": """ + resourceId + @""" " +
                                  @"                    }  " +
                                  @"                  ]  " +
                                  @"            }," +
                                  @"           ]" +
                                  @"       }" +
                                  @"}";

       
            EditAprimoRecord(requestBody, recordID);

            #endregion
        }

        // Find resource associated to Aprimo DAM recordID
        // Check if that resource has a file
        // If not - add the current master file (inRiver is a final destination for assets, so once a file is there it shouldnt need to be updated)
        // Update any metadata
        private void updateOperation(string recordID, string entityType, string entityID)
        {
            Context.Log(inRiver.Remoting.Log.LogLevel.Debug, $"Aprimo Environment - Updating with recordID {recordID}");
            // Get Access Token
            accessToken = GetDAMAccessToken();

            // Get Resource
            string inRiverFieldTypeID = Context.Settings["aprimoRecordIdFieldTypeID"];
            Entity inRiverResource = Context.ExtensionManager.DataService.GetEntityByUniqueValue(inRiverFieldTypeID, recordID, LoadLevel.DataOnly);

            // Variables
            dynamic recordBody = GetRecord(recordID);
            string masterFileName;
            string previewURI;
            string mimeType;

            // Check for file
            if (inRiverResource.MainPictureId == null)
            {

                if (recordBody["masterFileLatestVersion"] != null)
                {
                    Context.Log(inRiver.Remoting.Log.LogLevel.Debug, $"Aprimo Environment - masterfile found on record {recordID}");

                    masterFileName = recordBody["masterFileLatestVersion"]["fileName"];
                    previewURI = recordBody["preview"]["uri"];
                    // mimeType may come out as octet-stream regularly. You may need to add a file extension to mime type mapping
                    mimeType = recordBody["masterFileLatestVersion"]["fileType"]["mimeType"];

                    // Add file to inRiver Resource
                    Field resourceFileName = inRiverResource.GetField("ResourceFilename"); // Unique
                    Field resourceFileID = inRiverResource.GetField("ResourceFileId"); // Unique & requires a fileID obtained from creating a new ResourceFile using the UtilityService
                    Field resourceMimeType = inRiverResource.GetField("ResourceMimeType");

                    // Add file to inRiver and add metadata to Resource
                    int resourceFileId = Context.ExtensionManager.UtilityService.AddFileFromUrl(masterFileName, previewURI);
                    resourceFileName.Data = masterFileName;
                    resourceFileID.Data = resourceFileId;
                    resourceMimeType.Data = mimeType;

                    Context.Log(inRiver.Remoting.Log.LogLevel.Debug, $"Aprimo Environment - added file {masterFileName} to local resource {inRiverResource.Id}");

                }
                else
                {
                    // recordBody["masterFileLatestVersion"] must equal null
                    Context.Log(inRiver.Remoting.Log.LogLevel.Debug, $"Aprimo Environment - No masterfilefile on record {recordID} when trying to update resource {inRiverResource.Id}");

                }
            }


            // Update resource metadata with new Aprimo metadata
            foreach (dynamic obj in recordBody["fields"]["items"])
            {
                if (aprimoMetadataFieldsToMapToInRiverResource.ContainsKey((string)obj.fieldName))
                {
                    string inRiverFieldTypeId = aprimoMetadataFieldsToMapToInRiverResource[(string)obj.fieldName];
                    Field inRiverResourceField = inRiverResource.GetField(inRiverFieldTypeId);
                    inRiverResourceField.Data = (string)obj.localizedValues[0].value;
                    Context.Log(inRiver.Remoting.Log.LogLevel.Debug, $"Aprimo Environment - added data to local resource {inRiverResourceField.EntityId}");
                }

            }

            Context.Log(inRiver.Remoting.Log.LogLevel.Debug, $"Aprimo Environment - Updating resource {inRiverResource.Id} with Aprimo record {recordID}");
            Context.ExtensionManager.DataService.UpdateEntity(inRiverResource);
        }

        private void initializeDictionaries()
        {
            if(uniqueFieldDictionary == null)
            {
                // Split the entityUniqueFieldsForIdentifyng setting and turn it into a Dictionary at runtime. This way we can easily retrieve items in it moving forward.
                // This should only run once, when the connector starts
                uniqueFieldDictionary = Context.Settings["inRiverEntityUniqueFieldsForIdentifying"].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => part.Split(':'))
                    .ToDictionary(split => split[0], split => split[1]);
            }

            if (aprimoMetadataFieldsToMapToInRiverResource == null)
            {
                aprimoMetadataFieldsToMapToInRiverResource = Context.Settings["aprimoMetadataForResourceMapping"].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => part.Split(':'))
                    .ToDictionary(split => split[0], split => split[1]);
            }
        }
        // DAM Communication
        private string GetDAMAccessToken()
        {
            
            var retVal = "";

            // Form endpoint
            string accessTokenURL = String.Concat("https://" + $"{Context.Settings["aprimoTenant"]}" + ".aprimo.com/login/connect/token");

            using (HttpClient httpClient = new HttpClient())
            {

                // Set the URL and Headers
                httpClient.BaseAddress = new Uri(accessTokenURL);
                Dictionary<string, string> bodyParams = new Dictionary<string, string>()
                {
                    {"grant_type", "client_credentials" },
                    {"scope", "api" },
                    {"client_id", Context.Settings["clientID"] },
                    {"client_secret", Context.Settings["clientSecret"] }
                };


                // Make the call to get the access token
                HttpResponseMessage response = null;
                try
                {
                    response = httpClient.PostAsync(accessTokenURL, new FormUrlEncodedContent(bodyParams)).Result;

                    if (response.Content != null)
                    {
                        var responseContent = response.Content.ReadAsStringAsync().Result;
                        dynamic jsonObj = JsonConvert.DeserializeObject(responseContent);
                        retVal = jsonObj["access_token"];

                        if(accessToken == null)
                        {
                            // Start the stop watch. We'll get the elapsed time every time we make a request to Aprimo to determine if the access token has expired.
                            stopWatch.Start();
                        }
                        else
                        {
                            stopWatch.Reset();
                        }
                    }
                    else
                    {
                        throw new HttpRequestException();
                    }
                }
                catch (HttpRequestException e)
                {

                    // Error layer of inRiver logging will contain Status Codes and generic reasons from the server. 
                    // Debug layer will contain the request message or the stack trace
                    Context.Log(inRiver.Remoting.Log.LogLevel.Error, $"Aprimo Environment- Code {response.StatusCode}: {response.ReasonPhrase}");
                    Context.Log(inRiver.Remoting.Log.LogLevel.Debug, $"Aprimo Environment- Code {response.StatusCode}. \n Request: {response.RequestMessage}");
                }
                

               
            }
            return retVal;
        }
        private dynamic GetRecord(string recordID, int timeToWaitInMinutes = 0)
        {
            // Wait for a period of time before the next request so we dont overload Aprimo API
            if ( timeToWaitInMinutes <= 10)
            {
                System.Threading.Thread.Sleep(60000 * timeToWaitInMinutes);
            }
            else
            {
                // Waited 30 miuntes for the preview to finish processing. Something must have gone wrong.
                throw new TimeoutException($"Preview for masterfile for recordId {recordID} has not processed successfully after 30 minutes");
            }


            dynamic retVal = "";
            string recordEndpoint = String.Concat("https://" + $"{Context.Settings["aprimoTenant"]}" + $".dam.aprimo.com/api/core/record/{recordID}");

          
            using(HttpClient client = new HttpClient())
            {
                // If the access token has expired, get a new one
                if(hasAccessTokenExpired())
                {
                    
                    accessToken = GetDAMAccessToken();
                }
                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(recordEndpoint),
                    Method = HttpMethod.Get,
                    Headers =
                    {
                        { System.Net.HttpRequestHeader.Authorization.ToString(), $"Bearer {accessToken}" },
                        { "API-VERSION", "1" },
                        // mastefilelatestversion returns "fileState" that we use to determine if the preview has finished processing
                        // fields returns all metadata
                        // preview returns the download uri for the preview rendition
                        { "select-record", "masterfilelatestversion, fields, preview" },
                        { "select-fileversion", "filetype" },
                        { System.Net.HttpRequestHeader.Accept.ToString(), "application/json" },
                        { "User-Agent", "Aprimo inRiver Connector" },
                    },
                    Content = null,
                };
                HttpResponseMessage response = null;

                response = client.SendAsync(request).Result;

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = response.Content.ReadAsStringAsync().Result;
                    dynamic jsonObj = JsonConvert.DeserializeObject(responseContent);
                    retVal = jsonObj;

                }
                else
                {
                    Context.Log(inRiver.Remoting.Log.LogLevel.Error, $"Aprimo Environment- {response.StatusCode}: {response.ReasonPhrase}");
                    Context.Log(inRiver.Remoting.Log.LogLevel.Debug, $"Aprimo Environment- {response.RequestMessage}");
                }
            }
                        
            // Check masterfilelatestversion.fileState. If fileState != "Available" wait 1 minute and remake the call. Gradually increase 
            if (retVal["masterFileLatestVersion"]["fileState"] != "Available")
            {
                retVal = GetRecord(recordID, timeToWaitInMinutes + 2); // timeToWaitInMinutes will be 0, 2, 4, 8, 10, then throw an exception
            }
            
            return retVal;
        }
        // Takes a request body string and record ID and makes the request. Used for updating an Aprimo record with the inRiver resource ID it is associated it as well as exposing errors in Aprimo
        private void EditAprimoRecord(string requestBody, string recordId)
        {
            string editRecordUrl = String.Concat("https://" + $"{Context.Settings["aprimoTenant"]}" + $".dam.aprimo.com/api/core/record/{recordId}");
            using (HttpClient client = new HttpClient())
            {
                if(hasAccessTokenExpired())
                {
                    accessToken = GetDAMAccessToken();
                }
                var request = new HttpRequestMessage
                {
                    RequestUri = new Uri(editRecordUrl),
                    Method = HttpMethod.Put,
                    Headers =
                    {
                        { System.Net.HttpRequestHeader.Authorization.ToString(), $"Bearer {accessToken}" },
                        { "API-VERSION", "1" },
                        { System.Net.HttpRequestHeader.Accept.ToString(), "application/json" },
                        { "User-Agent", "Aprimo inRiver Connector" },
                    },
                    Content = new StringContent(requestBody, Encoding.UTF8, "application/json"),
                };

                HttpResponseMessage response = null;

                response = client.SendAsync(request).Result;

                if (response.StatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    HttpRequestException ex = new HttpRequestException($"{response.StatusCode} : {response.RequestMessage}");
                    throw ex;

                }
                else
                {
                    // Received Status Code 204 No Content. Which is Aprimo's success response on edit record requests
                }

            }
        }

        // inRiver Communication
        private Entity CreateResource(string fileName, string previewUri, string resourceTitle, string recordId, string mimeType)
        {
            Entity retVal;
            // Creates a resource in inRiver

            // Confirm that the resource does not already exist. We store the Aprimo Record ID as metadata on the resource, and if a resource already exists with the current record id 
            // then we do not try and create a new resource
            if (Context.ExtensionManager.DataService.GetEntityByUniqueValue(Context.Settings["aprimoRecordIdFieldTypeID"], recordId, LoadLevel.Shallow) == null)
            {
                // Get data model of the user chosen entity
                EntityType resourceEntityType = Context.ExtensionManager.ModelService.GetEntityType("Resource");
                // Stamp out the shell for a new entity using the data model
                Entity newResource = Entity.CreateEntity(resourceEntityType);

                // Upload  the file to inRiver as a ResourceFile and get a ResourceFileID
                // To upload a file to a Resource entity, you pass it the ResourceFileID of the file and inRiver will add it to the Resource
                int resourceFileId = Context.ExtensionManager.UtilityService.AddFileFromUrl(fileName, previewUri);

                // Fill in the new entity with data
                Field resourceName = newResource.GetField("ResourceName");
                Field resourceFileName = newResource.GetField("ResourceFilename"); // Unique
                Field resourceFileID = newResource.GetField("ResourceFileId"); // Unique & requires a fileID obtained from creating a new ResourceFile using the UtilityService
                Field resourceMimeType = newResource.GetField("ResourceMimeType");
                Field resourceFromAprimo = newResource.GetField(Context.Settings["aprimoResourceFromAprimoFieldTypeID"]);
                Field resourceAprimoId = newResource.GetField(Context.Settings["aprimoRecordIdFieldTypeID"]);

                resourceName.Data = resourceTitle;
                resourceFileName.Data = fileName;
                resourceFileID.Data = resourceFileId;
                resourceMimeType.Data = mimeType;
                resourceFromAprimo.Data = true;
                resourceAprimoId.Data = recordId;

                // Add the new Resource to inRiver. It can now be retrieved from the Context or found within the inRiver Enrich application
                retVal = Context.ExtensionManager.DataService.AddEntity(newResource);


            }
            else
            {
                throw new ArgumentException($"Aprimo Environment- Resource for {recordId} already exists"); // Caught in Add()
            }
            return retVal;
        }

        private Link CreateLink(Entity targetEntity, string EntityType, string EntityID)
        {
            // "[Entity]Resource" is best practice for link names and should be followed
            string LinkTypeName = string.Concat(EntityType, "Resource");

            // Get a unique identifier for the EntityType that we can search on
            string entityUniqueValue = uniqueFieldDictionary[EntityType];
            // Get Entity to link to. This entity will be the source of the link. Links are 1 way and we want the link to be from the source entity to the the resource
            Entity sourceEntity = Context.ExtensionManager.DataService.GetEntityByUniqueValue(entityUniqueValue, EntityID, LoadLevel.Shallow);

            // Create a Link between the preestablished entity and the newly created resource. This will include the resource within the entity's "media"
            LinkType entityResourceLinkType = Context.ExtensionManager.ModelService.GetLinkType(LinkTypeName);

            Link entityResourceLink = new Link()
            {
                Source = sourceEntity,
                Target = targetEntity,
                LinkType = entityResourceLinkType
            };

            entityResourceLink = Context.ExtensionManager.DataService.AddLink(entityResourceLink);

            return entityResourceLink;
        }

        // Misc.
        private bool hasAccessTokenExpired() => (stopWatch.Elapsed.Minutes >= 10) ? true : false;
        #endregion
    }
}
