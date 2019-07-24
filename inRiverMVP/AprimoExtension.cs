using Aprimo.InRiver.OutboundExtension;
using inRiver.Remoting;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
//using ResourceImport;
//using ServerExtension;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Aprimo.InRiver.InboundExtension
{
    class InboundExtensionTester
    {
        static RemoteManager _remoteManager;
        static ConsoleLogger _logger;
        static inRiverContext _context;
        static DataAPI dataAPI;
        static InRiverAprimoListener listener;

        static void Main(string[] args)
        {
            // Create RemoteManager, Logger, and get the inRiver context from the RemoteManager -> Context is effectively a wrapper to inRiver's internal API.
            Initialize();
            TestResourceCreate();
        }

        static void Initialize()
        {
            _remoteManager = RemoteManager.CreateInstance("https://partner.remoting.productmarketingcloud.com", "james.ratini@aprimo.com", "b6r1lLGMIClbiAKTlszG!");
            _logger = new ConsoleLogger();
            _context = new inRiverContext(_remoteManager, _logger);
           

            // Where can I find settings options? Are these just custom? https://servicecenter.inriver.com/hc/en-us/articles/360012462053-inRiver-Context
            /*Dictionary<string, string> settings = new Dictionary<string, string> {
            { "clientID", "RY3WL3AR-RY3W" },
            { "integrationUsername", "jratiniIntegration" },
            { "userToken", "270bd80a61274300bd5f0ed19d2d5f69" },
            { "aprimoTenant", "productstrategy1" },
            { "entityTypeDAMFieldID", "e101ff8be98d404ca43daa3f01538c97" }, //inRiverEntityType
            { "entityIDDAMFieldID", "6989a1abea754a3e887eaa8b0102c77b" }, // inRiverEntityID
            { "resourceIDDAMFieldID", "c871793508d34d1a97b9aa86010ca90a" },
            { "ResourceTitle", "Record Title" },
            { "aprimoRecordIdFieldTypeID", "AprimoRecordId" },
            { "aprimoResourceFromAprimoFieldTypeID", "ResourceFromAprimo" },
            { "aprimoInRiverStatusFieldID", "6e6ff917-37ef-42d7-949e-aa8c013cb2fe" },
            { "entityUniqueFieldsForIdentifying", "Product:ProductId;Item:ItemNumber;Channel:ChannelName" },
            { "inRiverProductMetadataMapping", "SKU:4661dee1-b4c8-4575-afbf-aa8c00edcd84;Materials:ce349f4e-0995-46dd-8934-aa880121abd9;Price:b47a5ce7-a364-4b6f-9eb5-aa8c00ee3fe8" }
            };*/

            Dictionary<string, string> InboundDataExtensionSettings = new Dictionary<string, string>
            {
                { "clientID", "RY3WL3AR-RY3W" },
                { "integrationUsername", "jratiniIntegration" },
                { "userToken", "270bd80a61274300bd5f0ed19d2d5f69" },
                { "aprimoTenant", "productstrategy1" },
                { "entityTypeDAMFieldID", "e101ff8be98d404ca43daa3f01538c97" }, //inRiverEntityType
                { "entityIDDAMFieldID", "7ac090e504944877ab4faa3f0153d467" }, // inRiverEntityID
                { "resourceIDDAMFieldID", "c871793508d34d1a97b9aa86010ca90a" },
                { "statusDAMFieldID",  "6e6ff917-37ef-42d7-949e-aa8c013cb2fe"},
                { "ResourceTitle", "Record Title" },
                { "aprimoRecordIdFieldTypeID", "AprimoRecordId" },
                { "aprimoResourceFromAprimoFieldTypeID", "ResourceFromAprimo" },
                { "entityUniqueFieldsForIdentifying", "Product:ProductId;Item:ItemNumber;Channel:ChannelName" }
            };
            Dictionary<string, string> ListenerExtensionSettings = new Dictionary<string, string>
            {
                { "clientID", "RY3WL3AR-RY3W" },
                { "integrationUsername", "jratiniIntegration" },
                { "userToken", "270bd80a61274300bd5f0ed19d2d5f69" },
                { "aprimoTenant", "productstrategy1" },
                { "aprimoResourceFromAprimoFieldTypeID", "ResourceFromAprimo" },
                { "aprimoRecordIdFieldTypeID", "AprimoRecordId" },
                { "inRiverProductMetadataMapping", "SKU:4661dee1-b4c8-4575-afbf-aa8c00edcd84;Materials:ce349f4e-0995-46dd-8934-aa880121abd9;Price:b47a5ce7-a364-4b6f-9eb5-aa8c00ee3fe8" },
                { "inRiverItemMetadataMapping", "inRiverField1:aprimoField1" }
            };

           
           
            dataAPI = new DataAPI();
            //listener = new InRiverAprimoListener();

            dataAPI.Context = _context;
            //listener.Context = _context;

            dataAPI.Context.Settings = InboundDataExtensionSettings;
            //listener.Context.Settings = ListenerExtensionSettings;

            
        }

        static string TestResourceCreate()
        {

            // Create a fake DAM asset (EntityType, File, Metadata?)
            // ChosenEntity is the type of inRiver entity the user wants to link the asset to in inRiver
            // EntityID is the unique identifier for the entity.
            string damData = "{ \"value\":\"27782ec4-abf4-4f84-8a8e-aa8701284419;crewNeckTeeShirt;Product;CREATE\" }";
            //int resourceId = _context.ExtensionManager.UtilityService.AddFileFromUrl("BURGUNDY.jpg","https://productstrategy1.dam.aprimo.com/api/core/download?token=z5i_B4Qz-N0GejqsBU9k6qDARinc6_OQUTTGXtozFrSQLOWrsYfGUAxviAh4AM9XLiAb2O7GiDPQVxA2VisihC6xIjrXvtV-F_YgHKmbeim3li_uCbNkxRl7Bj9hhNuyD1m26FHsT6gAm5m_VTI3HXc1ffc7H9Th4HoNRxF4iInvAW4xPJfkUrIaXD9ZzPPGmyzSY3N_ZMR647h3oYDS9H2XyHfMBb8T5bMGEKX4r87Y2Y1DPWeYBrQRw9kYxsfwxONCgs80P0uPRpy_ap1lcaKg60Y3xA1ualJf5PVqwrWRiQlXS2ZsTetx07tggCQr9y84PQKPKysDvF5dxkxb-Jjznt_8kWPoh_E4QwMES2CvcncEp2skVj-0IMiONQIry_RutbMyAS4H6J8Xfeo8FPL7rDtEEBBoEJAJGVLpa3I1");
            // Create an inRiver resource using the filename and file content
            string retVal = dataAPI.Add(damData);
            /* Entity ent = _context.ExtensionManager.DataService.GetEntity(32, LoadLevel.DataAndLinks);
             ent.OutboundLinks.ForEach(delegate (Link item)
             {
                 Console.WriteLine($"link id: {item.Id}. source: {item.Source.Id}. target: {item.Target.Id}. linktype: {item.LinkType}");
                 listener.LinkCreated(item.Id, item.Source.Id, item.Target.Id, item.LinkType.ToString(), null);
             });*/
            //string[] fields = { "SKU", "Price" };
            //listener.EntityUpdated(32, fields);

            Console.WriteLine("Waiting for key press...");
            Console.ReadKey();
            return "test";
        }

    }
}
