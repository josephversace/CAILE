using IIM.Shared.Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IIM.Shared.Models
{  
    
    public class VirtualFolder
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public List<VirtualFile> Files { get; set; } = new();
        public List<VirtualFolder> SubFolders { get; set; } = new();
    }


 

}
