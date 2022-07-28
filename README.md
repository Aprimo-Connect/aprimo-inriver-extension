# aprimo-inriver-extension

## inRiver Model Assumptions
It is assumed that the following is true for your inRiver model to be able to use the existing connector code. If you have differences in your model, you may have to change the connector code to get the functionality outlined in this document. 
- The special EntityType of Resource exists along with the following fields. 

 ![modelassumption1](https://user-images.githubusercontent.com/51798256/181623925-1f7846bb-a7ce-4fe1-bc01-c4cce5608d0a.jpg)

    - ResourceFromAprimo is marked Mandatory 
    - AprimoRecordId is marked Unique 
-	The entities you are linking Aprimo assets to have a unique identifier configured and can be linked to Resources.
-	All link names to the resource entity will be in the best practice format “[EntityType]Resource”. E.g. “ItemResource”, “ProductResource”
-	For every EntityType you would like to be able to link Aprimo assets to, you must include a metadata mapping in the EntityListener and LinkListener extensions settings. Use the following format “inRiverFieldTypeId:AprimoFieldID;inRiverFieldTypeId2:AprimoFieldID2”. You must also go into the Extension’s code and create a “private Dictionary<string, string> properlyNamedDictionary = null;” as a class variable for the InRiverAprimoListener class.

![code1](https://user-images.githubusercontent.com/51798256/181624312-78e11d81-d97b-4cfa-a3a3-7e0014f5ea2a.jpg)
-	Add the new dictionary to the initializeDictionaries() function. Reuse the code, changing the index string of the Context.Settings to the appropriate setting, to convert the mapping to a dictionary. 


![code2](https://user-images.githubusercontent.com/51798256/181624356-0544e979-d39d-47f1-bb7c-438d2290a393.jpg)
-	Add the dictionary to the determineMetadataDictionary(string entitytype) function. This function is used by the LinkCreated and ConstructAddOrUpdateObjects() functions to identify which metadata mapping the integration should use for the specific entity it is working on.

![code3](https://user-images.githubusercontent.com/51798256/181624424-1586fa2f-b896-448e-90a8-b6657a2ab399.jpg)

## Activation
This section will go over the process of implementing the Aprimo inRiver Connector.

1.	Download the Connector solution from Aprimo
2.	Log into Aprimo and follow the step 1 under Retrieving an Access Token for Services and Daemons to create an integration registration. Note the client id.
3.	In Aprimo, create an integration user who has rights to read and write to all assets that may sync with inRiver, and note the username and user token (see Step 2 under Retrieving an Access Token for Services and Daemons)
4.	Navigate the Aprimo DAM System UI and create the following fields (you can change the name without worry)
    -	inRiverEntityType – Option List – Set the options to be the different inRiver entities you would like the connector to be able to link resources to (e.g. Product, Item, Look, etc.)
    -	inRiverEntityId – Text – The user will enter the unique Id of the entity they want the resource linked to.
    -	InRiverResourceId – Numeric – The field will expose the entity ID of the resource in inRiver associated to this record
    -	InRiverStatus
    -	Create fields for any custom metadata you want synced back from inRiver (see configurability for supported types below). Any custom field must be Record Content Type Dependent
5.	Compile the connector. Go to the connector bin directory and zip the following files into “Aprimo.InRiver.InboundExtension.zip”.
    -	Antlr3.Runtime.dll
    -	Aprimo.InRiver.InboundExtension.dll
    -	inRiver.Remoting.dll
    -	Newtonsoft.Json.dll
    -	Newtonsoft.Json.xml
6.	Go to your inRiver Control Center’s ‘Connect’ tab
7.	Navigate to the Packages section
8.	Upload the .zip file
9.	Navigate back to Connect and navigate to Extensions
10.	Create a new extension using the package you just uploaded
    - Choose a descriptive Extension Id and apiKey
![AprimoStarterKit](https://user-images.githubusercontent.com/51798256/181625315-8194b524-5a3a-430e-b689-caa30abfbcdf.jpg)
11.	Repeat steps 5 – 10 for the Aprimo.InRiver.OutboundExtensions(EntityListener and LinkListener)
    -	You won’t need an apiKey for the OutboundExtensions
12.	Create a new Business Rule in Aprimo DAM (System > Advanced > Create New Rule)
13.	Set the conditions of the rule so it executes when someone sets the inRiverEntityType and inRiverEntityId. 
    - E.G.
    - ![aprimoruleconfig](https://user-images.githubusercontent.com/51798256/181625523-db3336f1-92a9-439d-ba53-569f6558c7f7.jpg)
14.	The rule’s action should execute the following reference
    -	To get the Basic Authentication Token for @apiKey base64 encode apikey:[InboundDataExtension’s apikey] without the brackets
    -	To get the uri see the documentation for InboundDataExtension

```
<ref:record out="id" store="@recordID" />
<ref:text out="Basic [Basic Auth Token]" store="@apiKey" />
<ref:record fieldId="e101ff8be98d404ca43daa3f01538c97" out="valuename" store="@entityType"/>
<ref:record fieldId="7ac090e504944877ab4faa3f0153d467" out="value" store="@entityUniqueID"/>

<ref:httpRequest uri="[InboundDataExtension POST URL] " include="auth-code" timeout="15">
<Request>
<Headers>
   <Header name="Authorization">@apiKey</Header>
</Headers>
<Body>
{
"value":<ref:text out="@recordID;@entityUniqueID;@entityType;CREATE" encode="json" />
}
</Body>
</Request>
</ref:httpRequest>
```

## Design Considerations
Below are a list of features and design considerations that may impact you as you complete your integration. 
**Changing relationships from M:1 Aprimo Asset:inRiver Entity to M:M Aprimo Asset:inRiver**
The connector assumes that a user will associate a single entity in inRiver to an asset in Aprimo. In the cases where an asset may represent multiple entities (i.e. a model wearing multiple products), it is expected that that asset would tie to a “look” type entity that represents multiple entities in a collection. 
However, if you choose to allow an asset to be associated to multiple entities, you will need to:
-	Change fields in the DAM to allow users to select multiple entities to tie to in Aprimo
-	Adjust Interface A to create multiple links on the resource created
-	Change how metadata is written back into Aprimo to accommodate that multiple entities may need to synchronize back to Aprimo (or choose a priority order for which entity’s metadata gets written back to Aprimo).


**Reacting to additional events in Aprimo**
The connector will only allow a user to set the inRiver entity and unique Id once. Consider implementing the following actions for the following triggers:
-	Updated Asset Case
  -	Trigger: A user logs into the DAM and changes the inRiver entity type or inRiver unique Id of on the asset record, attempting to point the asset to different entity in inRiver. 
  -	Action: Change the link in the PIM to point to the new item.
  -	Technical Design: Create a DAM rule to listen for the trigger changes, which invokes a custom InboundDataExtension, passing along the DAM record Id. Call back into the DAM via the REST API and adjust the link in inRiver to point to the newly specified entity.
-	New Asset Version Case
  -	Trigger: A user logs into the DAM and adds a new file version to the master file on a record which is synched with inRiver.
  -	Action: Update the resource in inRiver with the latest file version.
  -	Technical Design: Create a DAM rule to listen for the trigger changes, which invokes a custom InboundDataExtension, passing along the DAM record Id. Call back into the DAM via the REST API to get an updated preview and update the resource in inRiver.
-	Deleted/Expired Asset Case
  -	Trigger: A user deletes or expires an asset When an asset is deleted or expired
  -	Action: Soft delete the resource record in PIM via an active/inactive flag.
  -	Technical Design: Create a DAM rule to listen for the trigger changes, which invokes a custom InboundDataExtension, passing along the DAM record Id. Update the resource in InRiver to be inactive or remove it.
Note that the DAM only sends POST requests, so the value that is passed to the InboundDataExtension must also contain an identifier for the desired action.

**Beware the Infinite Loop**
Be careful to avoid an infinite loop – Asset updates in Aprimo may trigger callouts to inRiver, and entity updates in inRiver may update assets in Aprimo. Ensure your rule conditions and listener extension code does not cause an infinite loop of updates. 
**Leverage Connector State for Robustness**
Leverage the inRiver [connector state](https://servicecenter.inriver.com/hc/en-us/articles/360012553853-Connector-State) to retry any requests to Aprimo that had failed. The connector contains a ConnectorStateHelper class that acts as a wrapper around inRiver’s ConnectorState class. The ConnectorState is useful for sharing data between different extensions. The connector does not make use of this fully, but if an API request to Aprimo to edit a record fails, the connector will log the error and use ConnectorStateHelper to store the request. You can expand this functionality and implement a feature that can retry failed requests on a schedule.
**Leverage Connector State for Scalability**
Additionally, for implementations that may have large amount of updates, it’s recommended to change Interface B to process on a schedule instead of real time. To do this, modify the ListenerExtension to log changes to ConnectorState, and processes the changes in a ScheduledExtension instead, de-duping any ConnectorState messages pointing to the same entity.
**Additional Languages**
English is the only supported language for the connector. If additional language support is needed, this will have to be built in.



