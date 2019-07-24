using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aprimo.InRiver.OutboundExtension
{
    public struct MessageTypes
    {
        public static string EntityCreated = "EntityCreated";
        public static string EntityUpdated = "EntityUpdated";
        public static string LinkCreated = "LinkCreated";
        public static string LinkDeleted = "LinkDeleted";
    }

    public class ConnectorStateHelper
    {
        private static ConnectorStateHelper instance;

        private ConnectorStateHelper()
        { }
        public static ConnectorStateHelper Instance => instance ?? (instance = new ConnectorStateHelper());

        public void Save(string connectorId, HttpRequestMessage message, inRiverContext Context)
        {
            Context.ExtensionManager.UtilityService.AddConnectorState(new ConnectorState { ConnectorId = connectorId, Data = JsonConvert.SerializeObject(message) });
        }

        public void Clear(string connectorId, inRiverContext Context)
        {
            Context.ExtensionManager.UtilityService.DeleteConnectorStates(connectorId);
        }
        public List<string> GetAllMessages(string connectorId, inRiverContext Context)
        {
            List<ConnectorState> states = Context.ExtensionManager.UtilityService.GetAllConnectorStatesForConnector(connectorId);

            if (!states.Any())
            {
                return new List<string>();
            }

            List<string> messages = states.Select(s => JsonConvert.DeserializeObject<string>(s.Data)).ToList();

            Context.ExtensionManager.UtilityService.DeleteConnectorStates(states.Select(s => s.Id).ToList());

            return messages;
        }

        public List<string> PeakAllMessages(string connectorId, inRiverContext Context)
        {
            List<ConnectorState> states = Context.ExtensionManager.UtilityService.GetAllConnectorStatesForConnector(connectorId);

            if (!states.Any())
            {
                return new List<string>();
            }

            List<string> messages = states.Select(s => JsonConvert.DeserializeObject<string>(s.Data)).ToList();

            return messages;
        }
    }
}
