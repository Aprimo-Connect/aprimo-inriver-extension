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
            TestInbound();
            //TestOutbound();
        }

        static void Initialize()
        {
            _remoteManager = RemoteManager.CreateInstance("https://partner.remoting.productmarketingcloud.com", [Username], [Password]);
            _logger = new ConsoleLogger();
            _context = new inRiverContext(_remoteManager, _logger);
           

            
            Dictionary<string, string> InboundDataExtensionSettings = new Dictionary<string, string>
            {
                { "clientID", "[ClientID]" },
                { "clientSecret","[ClientSecret" },
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
            Dictionary<string, string> ListenerExtensionSettings = new Dictionary<string, string>
            {
                { "clientID", "[ClientID]" },
                { "clientSecret", "clientSecret" },
                { "integrationUsername", "[AprimoUsername]" },
                { "aprimoTenant", "[AprimoTenantName]" },
                { "aprimoResourceFromAprimoFieldTypeID", "[inRiverFieldNameForFieldToMarkAResourceFromAprimo]" },
                { "aprimoRecordIdFieldTypeID", "[inRiverFieldNameToStoreAprimoRecordID]" },
                { "inRiverProductMetadataMapping", "[ProductMetadataMapping]" }, //EX: SKU:[AprimoFieldID];Materials:[AprimoFieldID];Price:[AprimoFieldID]
                { "inRiverItemMetadataMapping", "[ItemMetadataMapping]" }
            };

           // DataAPI is for the InboundDataExtension and InRiverAprimoListener is for the OutboundExtension
           // DataAPI calls the Add() method - which is what Aprimo rules will call during the integration
           // InRiverAprimoListener acts as an inRiver instance and calls the methods that inRiver calls when entities in inRiver are create/update/deleted/etc
           
            dataAPI = new DataAPI();
            listener = new InRiverAprimoListener();

            dataAPI.Context = _context;
            listener.Context = _context;

            dataAPI.Context.Settings = InboundDataExtensionSettings;
            listener.Context.Settings = ListenerExtensionSettings;

            
        }

        static string TestInbound()
        {

            // Inbound Test
            string damData = "{ \"value\":\"[aprimoRecordID];[inRiverEntityName];[inRiverEntityType];ProcessName(CREATE or UPDATE)\" }";
            
            // Create an inRiver resource using the filename and file content
            string retVal = dataAPI.Add(damData);

           


            Console.WriteLine("Waiting for key press...");
            Console.ReadKey();
            return "test";
        }

        static string TestOutbound()
        {
            //Outbound
            listener.EntityUpdated(123, new string[3] { "field1", "field2", "field3" });
            return "test";
        }
    }
}
