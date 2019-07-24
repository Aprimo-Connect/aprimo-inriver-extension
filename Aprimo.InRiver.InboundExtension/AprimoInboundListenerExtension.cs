using inRiver.Remoting.Extension.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;

namespace Aprimo.InRiver.InboundExtension
{
    public class InRiverAprimoListener : IEntityListener
    {
        public inRiverContext Context { get; set; }

        public Dictionary<string, string> DefaultSettings => new Dictionary<string, string>()
        {

        };

        public void EntityCommentAdded(int entityId, int commentId)
        {
            throw new NotImplementedException();
        }

        public void EntityCreated(int entityId)
        {
            throw new NotImplementedException();
        }

        public void EntityDeleted(Entity deletedEntity)
        {
            throw new NotImplementedException();
        }

        public void EntityFieldSetUpdated(int entityId, string fieldSetId)
        {
            throw new NotImplementedException();
        }

        public void EntityLocked(int entityId)
        {
            throw new NotImplementedException();
        }

        public void EntitySpecificationFieldAdded(int entityId, string fieldName)
        {
            throw new NotImplementedException();
        }

        public void EntitySpecificationFieldUpdated(int entityId, string fieldName)
        {
            throw new NotImplementedException();
        }

        public void EntityUnlocked(int entityId)
        {
            throw new NotImplementedException();
        }

        public void EntityUpdated(int entityId, string[] fields)
        {
            throw new NotImplementedException();
        }

        public string Test()
        {
            throw new NotImplementedException();
        }
    }
}
