using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;

namespace Hangfire.MySql.src.Entities
{
    [Table]
    internal class JobParameter
    {
        [PrimaryKey]
        public int Id { get; set; }
        [Column]
        public int JobId { get; set; }
        [Column]
        public string Name { get; set; }
        [Column]
        public string Value { get; set; }
    }

}
