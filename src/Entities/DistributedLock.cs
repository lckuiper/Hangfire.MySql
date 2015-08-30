using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;

namespace Hangfire.MySql.src.Entities
{
    [Table]
    internal class DistributedLock
    {
        [PrimaryKey]
        public long Id { get; set; }

        [Column]
        public string Resource { get; set; }

    }
}
