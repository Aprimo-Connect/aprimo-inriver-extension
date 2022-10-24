using inRiver.Remoting.Extension;
using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

// The OutboundExtension class library contains the listener extension that receives changes to inRiver and makes requests to Aprimo
// and the scheduler extension that attempts to retry the requests to Aprimo that the listener failed on
namespace Aprimo.InRiver.OutboundExtension
{
    public class InRiverAprimoListener : IEntityListener, ILinkListener
    {
       
        public inRiverContext Context { get; set; }

        private string connectorStateIdSettingKey = "AprimoListenerConnectorState";
        public Dictionary<string, string> DefaultSettings => new Dictionary<string, string>()
        {
            { "clientID", "[ClientID]" },
            { "clientSecret", "[ClientSecret" },
            { "integrationUsername", "[IntegrationUsername]" },
            { "aprimoTenant", "[AprimoTenantName]" },
            { "aprimoResourceFromAprimoFieldTypeID", "[inRiverFieldNameForResourceFromAprimo]" },
            { "aprimoRecordIdFieldTypeID", "[inRiverFieldNameForRecordID]" },
            { "inRiverProductMetadataMapping", "[ProductMetadataMapping]" }, // Ex. SKU:[aprimofieldid];Materials:[aprimofieldid];Price:[aprimofieldid]
            { "inRiverItemMetadataMapping", "[ItemMetadataMapping]" }
           
        };

        Dictionary<string, string> productMetadataMapping = null;
        Dictionary<string, string> itemMetadataMapping = null;
        public InRiverAprimoListener()
        {
           
            
        }
        public InRiverAprimoListener(inRiverContext _context)
        {
            Context = _context;
        }

        #region IEntityListener
        public void EntityCommentAdded(int entityId, int commentId)
        {
            
        }

        public void EntityCreated(int entityId)
        {

        }

        public void EntityDeleted(Entity deletedEntity)
        {
            
        }

        public void EntityFieldSetUpdated(int entityId, string fieldSetId)
        {
            
        }

        public void EntityLocked(int entityId)
        {
            
        }

        public void EntitySpecificationFieldAdded(int entityId, string fieldName)
        {
            
        }

        public void EntitySpecificationFieldUpdated(int entityId, string fieldName)
        {
            
        }

        public void EntityUnlocked(int entityId)
        {
            
        }

        // If we update inRiver when Aprimo is updated we could hit an infinite loop here if we don't validate that the user is NOT the integration account.
        // If it is the integration account, ignore the update.
        public void EntityUpdated(int entityId, string[] fields)
        {
            initializeDictionaries();
            //string[] fields contains the field names that were updated. Does not contain their new values
            Context.Log(inRiver.Remoting.Log.LogLevel.Debug, $"ProductStrategy1- Entity {entityId.ToString()} updated with fields {fields}");

            // Get all entity Links
            Entity updatedEntity = Context.ExtensionManager.DataService.GetEntity(entityId, LoadLevel.DataAndLinks);
            List<Link> entityLinks = updatedEntity.OutboundLinks;
            string accessToken = GetDAMAccessToken();
            int resourceId;

            entityLinks.ForEach(delegate (Link item)
            {
                resourceId = item.Target.Id;
                Entity currEntity = Context.ExtensionManager.DataService.GetEntity(resourceId, LoadLevel.DataOnly);
                if((bool)currEntity.GetField(Context.Settings["aprimoResourceFromAprimoFieldTypeID"])?.Data == true)
                {
                    Console.WriteLine("Found Aprimo resource link");
                    // When we load the updated entity's data and links, we do not get the data for the entities it is linked to. We have to get that data from inRiver seperately
                    string recordId = currEntity.GetField(Context.Settings["aprimoRecordIdFieldTypeID"]).Data.ToString();

                    // Construct edit record body
                    string fieldsToUpdate = ConstructAddOrUpdateObjects(updatedEntity, fields);
                    var requestBody = @"{" +
                                      @"     ""fields"":  " +
                                      @"       {" +
                                      @"          ""addOrUpdate"":[" + fieldsToUpdate + "]" +
                                      @"       }" +
                                      @"}";

                    // Make request to Aprimo to update record
                    try
                    {
                        EditAprimoRecord(requestBody, recordId, accessToken);
                    }
                    catch (Exception e)
                    {
                        // Add request to scheduler to try again later
                        // For the Aprimo extension leave it at this. If a customer wants to implement this further, they can.
                        Console.WriteLine(e.Message);
                        ConnectorStateHelper.Instance.Save(connectorStateIdSettingKey, (HttpRequestMessage)e.Data["Request"], Context);
                        Context.Log(inRiver.Remoting.Log.LogLevel.Debug, "ProductStrategy1- Created Connector State");
                    }
                }
            });
        }

        public string Test()
        {
            return "Testing Listener Extension!";
        }
        #endregion
        #region ILinkListener
        public void LinkCreated(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
        {
            Context.Log(inRiver.Remoting.Log.LogLevel.Debug, "ProductStrategy1- Link Created() Called");
            // The function will execute when any link is created.
            // Check the target(resource) to determine if it is an aprimo resource
            Entity targetResource = Context.ExtensionManager.DataService.GetEntity(targetId, LoadLevel.DataOnly);
            if((bool)targetResource.GetField(Context.Settings["aprimoResourceFromAprimoFieldTypeID"])?.Data == true)
            {
                initializeDictionaries();
                // If it is, get the record id from the resource
                string recordId = (string)targetResource.GetField(Context.Settings["aprimoRecordIdFieldTypeID"]).Data;

                // Load in the source entity and get a list of all fields on the entity
                Entity sourceEntity = Context.ExtensionManager.DataService.GetEntity(sourceId, LoadLevel.DataOnly);
                List<Field> entityFields = sourceEntity.Fields;

                Dictionary<string, string> metadataMappings = null;
                metadataMappings = determineMetadataDictionary(sourceEntity.EntityType.ToString());

                string fieldsToUpdate = "";

                entityFields.ForEach(delegate (Field item)
                {
                    if (metadataMappings.ContainsKey(item.ToString()) && metadataMappings[item.ToString()] != null && item.Data != null)
                    {
                        fieldsToUpdate += @"{" +
                                          @"     ""id"": """ + metadataMappings[item.ToString()] + @""",     " +
                                          @"     ""localizedValues"":[" +
                                          @"         {  " +
                                          @"            ""languageId"":""00000000000000000000000000000000"", " +
                                          @"            ""value"": """ + item.Data + @""" " +
                                          @"         }  " +
                                          @"       ]  " +
                                          @"},";
                    }
                });
                fieldsToUpdate = fieldsToUpdate.Remove(fieldsToUpdate.Length - 1);


                var requestBody = @"{" +
                                  @"     ""fields"":  " +
                                  @"       {" +
                                  @"          ""addOrUpdate"":[" + fieldsToUpdate + "]" +
                                  @"       }" +
                                  @"}";


                // Get an aprimo access token
                string accessToken = GetDAMAccessToken();

                // Make a request to Aprimo to update the record
                EditAprimoRecord(requestBody, recordId, accessToken);
            }

            
        }

        public void LinkUpdated(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
        {
            
        }

        public void LinkDeleted(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
        {
            
        }

        public void LinkActivated(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
        {
            
        }

        public void LinkInactivated(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
        {
            
        }
        #endregion
        #region Helper Functions
        private void initializeDictionaries()
        {
            // It is generally easier to work with a well defined data structure than it is to work with a string with multiple delimiters. For future functionality converting the
            // metadata mappings to a data structure may be best
            // If the dictionary is not null, then there is no need to initialize it.
            if(productMetadataMapping == null)
            {
                productMetadataMapping = Context.Settings["inRiverProductMetadataMapping"].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(UriPartial => UriPartial.Split(':'))
                    .ToDictionary(split => split[0], split => split[1]);
            }

            // Add new dictionary metadata mappings here.
        }
        private bool LoadResourceAndEvaluate(Link resourceLink)
        {
            bool retVal = false;
            int resourceId = resourceLink.Target.Id;

            // If the link's target is a resource and it's ResourceFromAprimo field is set to 'true'
            if (resourceLink.Target.EntityType.Id == "Resource" && Context.ExtensionManager.DataService.GetEntity(resourceId, LoadLevel.DataAndLinks).GetField(Context.Settings["aprimoResourceFromAprimoFieldTypeID"]).Data.Equals(true))
            {
                retVal = true;
            }
            return retVal;
        }
        private string ConstructAddOrUpdateObjects(Entity updatedEntity, string[] fields)
        {
            Dictionary<string,string> metadataMappings = null;
            metadataMappings = determineMetadataDictionary(updatedEntity.EntityType.ToString());

            string fieldsToUpdate = "";
           
            for(int i = 0; i < fields.Length; i++)
            {
                // Ensure the mapping exists, the field exists on the entity, and the data is not null
                if(metadataMappings[fields[i]] != null && updatedEntity.GetField(fields[i]) != null && updatedEntity.GetField(fields[i]).Data != null)
                {
                    Console.WriteLine($"Adding id: {metadataMappings[fields[i]]} and value: {updatedEntity.GetField(fields[i]).Data} to put request");
                    fieldsToUpdate += @"{" +
                                      @"     ""id"": """ + metadataMappings[fields[i]] + @""",     " +
                                      @"     ""localizedValues"":[" +
                                      @"         {  " +
                                      @"            ""languageId"":""00000000000000000000000000000000"", " +
                                      @"            ""value"": """ + updatedEntity.GetField(fields[i]).Data + @""" " +
                                      @"         }  " +
                                      @"       ]  " +
                                      @"},";
                }
            }
            

            // Remove the very last character of fieldToUpdate as it is a comma that will cause trouble
            return fieldsToUpdate.Remove(fieldsToUpdate.Length - 1);
        }
        private Dictionary<string,string> determineMetadataDictionary(string entityType)
        {
            Dictionary<string, string> retVal = null;
            switch (entityType)
            {
                case "Product":
                    retVal = productMetadataMapping;
                    break;
                case "Item":
                    retVal = itemMetadataMapping;
                    break;
            }

            return retVal;
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
                    Context.Log(inRiver.Remoting.Log.LogLevel.Error, $"ProductStrategy1- Code {response.StatusCode}: {response.ReasonPhrase}");
                    Context.Log(inRiver.Remoting.Log.LogLevel.Debug, $"ProductStrategy1- Code {response.StatusCode}. \n Request: {response.RequestMessage}");
                }



            }
            return retVal;
        }
        private void EditAprimoRecord(string requestBody, string recordId, string accessToken)
        {
            string editRecordUrl = String.Concat("https://" + $"{Context.Settings["aprimoTenant"]}" + $".dam.aprimo.com/api/core/record/{recordId}");
            using (HttpClient client = new HttpClient())
            {
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

                
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    // Aprimo returns 204 No Content on successful record edit requests.
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Record does not exist. Fail, but don't add the request to a connector state
                }
                else
                {
                    HttpRequestException ex = new HttpRequestException();
                    ex.Data["Request"] = request.ToString();
                    throw ex;
                }
                
            }
        }
        #endregion
       


    }

   
}
