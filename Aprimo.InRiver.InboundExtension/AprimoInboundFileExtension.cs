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
        }
        
        // POST
        public string Add(string filename, byte[] bytes)
        {

            return $"Created resource";
        }

        //DELETE
        public string Delete(string filename)
        {
            return $"Aprimo Environment- echo {filename}";
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
