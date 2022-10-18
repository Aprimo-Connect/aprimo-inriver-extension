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
           

            
            Dictionary<string, string> InboundDataExtensionSettings = new Dictionary<string, string>
            {
                { "clientID", "[ClientID]" },
                { "clientSecret","[ClientSecret" },
                { "integrationUsername", "[IntegrationUsername]" },
                { "userToken", "[IntegrationUserToken]" },
                { "aprimoTenant", "[AprimoTenantName]" },
                { "entityTypeDAMFieldID", "[AprimoFieldIDForEntityType]" }, //inRiverEntityType
                { "entityIDDAMFieldID", "[AprimoFieldIDForEntityID]" }, // inRiverEntityID
                { "resourceIDDAMFieldID", "[AprimoFieldIDForResourceID]" },
                { "statusDAMFieldID",  "[AprimoFieldIDForStatusMessages]"},
                { "ResourceTitle", "[AprimoFieldToUseForInRiverResourceTitle]" },
                { "aprimoRecordIdFieldTypeID", "[inRiverFieldNameToStoreAprimoRecordID]" },
                { "aprimoResourceFromAprimoFieldTypeID", "[inRiverFieldNameForFieldToMarkAResourceFromAprimo]" },
                { "entityUniqueFieldsForIdentifying", "[OptionsListMapping]" } //EX: Product:ProductId;Item:ItemNumber;Channel:ChannelName

                
            };
            Dictionary<string, string> ListenerExtensionSettings = new Dictionary<string, string>
            {
                { "clientID", "[ClientID]" },
                { "integrationUsername", "[AprimoUsername]" },
                { "userToken", "[AprimoUserToken]" },
                { "aprimoTenant", "[AprimoTenantName]" },
                { "aprimoResourceFromAprimoFieldTypeID", "[inRiverFieldNameForFieldToMarkAResourceFromAprimo]" },
                { "aprimoRecordIdFieldTypeID", "[inRiverFieldNameToStoreAprimoRecordID]" },
                { "inRiverProductMetadataMapping", "[ProductMetadataMapping]" }, //EX: SKU:[AprimoFieldID];Materials:[AprimoFieldID];Price:[AprimoFieldID]
                { "inRiverItemMetadataMapping", "[ItemMetadataMapping]" }
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
            string damData = "{ \"value\":\"cabef556-986f-4c9d-8253-af32011a37a6;prodStratUnitTest;Product;CREATE\" }";
            
            // Create an inRiver resource using the filename and file content
            string retVal = dataAPI.Add(damData);
           

            Console.WriteLine("Waiting for key press...");
            Console.ReadKey();
            return "test";
        }

    }
}
