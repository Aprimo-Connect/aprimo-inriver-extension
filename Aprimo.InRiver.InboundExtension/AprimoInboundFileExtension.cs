using inRiver.Remoting;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
//using ResourceImport;
//using ServerExtension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;


namespace Aprimo.InRiver.InboundExtension
{
    public class FileAPI : inRiver.Remoting.Extension.Interface.IInboundFileExtension
    {
        public inRiverContext Context { get; set; }

        public Dictionary<string, string> DefaultSettings => new Dictionary<string, string>();
        public  FileAPI()
        {
            DefaultSettings.Add("Setting1", "Value1");
        }
        
        // POST
        public string Add(string filename, byte[] bytes)//, string entityTypeToLinkTo, string entityTypeToLinkToID)
        {
            #region 1. Resource Creation
            // Creates a resource in inRiver

            // Get data model of the user chosen entity
            EntityType resourceEntityType = Context.ExtensionManager.ModelService.GetEntityType("Resource");

            // Stamp out the shell for a new entity using the data model
            Entity newResource = Entity.CreateEntity(resourceEntityType);

            // Upload  the file to inRiver as a ResourceFile and get a ResourceFileID
            // To upload a file to a Resource entity, you pass it the ResourceFileID of the file and inRiver will add it to the Resource
            int resourceFileId = Context.ExtensionManager.UtilityService.AddFile(filename, bytes);

            // Fill in the new entity with data
            Field resourceName = newResource.GetField("ResourceName");
            Field resourceFileName = newResource.GetField("ResourceFilename"); // Unique
            Field resourceFileID = newResource.GetField("ResourceFileId"); // Unique & requires a fileID obtained from creating a new ResourceFile using the UtilityService
            Field resourceMimeType = newResource.GetField("ResourceMimeType");

            resourceName.Data = "jratiniTest";
            resourceFileName.Data = filename;
            resourceFileID.Data = resourceFileId;
            resourceMimeType.Data = "image/jpg";

            // Add the new Resource to inRiver. It can now to retrieved from the Context or found within the inRiver Enrich application
            var resource = Context.ExtensionManager.DataService.AddEntity(newResource);
            #endregion
            #region 2. Fake Linking

            // Testing variables. Still need a solution on how to get this information from the DAM to this method.
            string EntityType = "Product";
            string EntityTypeID = "JamesNewProduct01";
            string LinkTypeName = string.Concat(EntityType, "Resource");

            // Get Entity to link to. This entity will be the source of the link. Links are 1 way and we want the link to be from the source entity to the the resource
            Entity sourceEntity = Context.ExtensionManager.DataService.GetEntityByUniqueValue(EntityType == "Product" ? "ProductId" : "ItemNumber", EntityTypeID, LoadLevel.DataAndLinks);

            // Create a Link between the preestablished entity and the newly created resource. This will include the resource within the entity's "media"
            LinkType entityResourceLinkType = Context.ExtensionManager.ModelService.GetLinkType(LinkTypeName);

            Link entityResourceLink = new Link()
            {
                Source = sourceEntity,
                Target = resource,
                LinkType = entityResourceLinkType
            };

            entityResourceLink = Context.ExtensionManager.DataService.AddLink(entityResourceLink);

            #endregion

            return $"Created resource {resourceFileName} and linked to {EntityTypeID}";
        }

        //DELETE
        public string Delete(string filename)
        {
            return $"echo {filename}";
        }

        //GET
        public string Test()
        {
            return "Hello, World";
        }
        //PUT
        public string Update(string filename, byte[] bytes)
        {
            throw new NotImplementedException();
        }

       
    }
}
