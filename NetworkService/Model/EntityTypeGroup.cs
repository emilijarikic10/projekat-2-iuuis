using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace NetworkService.Model
{
    public class EntityTypeGroup
    {
        public string TypeName { get; set; }
        public ObservableCollection<ServerEntity> Entities { get; set; }

        public EntityTypeGroup(string typeName)
        {
            TypeName = typeName;
            Entities = new ObservableCollection<ServerEntity>();
        }
    }
}
