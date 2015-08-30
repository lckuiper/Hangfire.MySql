using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;

namespace Hangfire.MySql.src.Entities
{

    // TODO: should this be called MySqlJob - check with SqlStorage

    [Table]
    internal class Job
    {
        [PrimaryKey]
        public int Id { get; set; }
        [Column]
        public string InvocationData { get; set; }
        [Column]
        public string Arguments { get; set; }
        [Column]
        public DateTime CreatedAt { get; set; }
        [Column]
        public DateTime? ExpireAt { get; set; }
        [Column]
        public DateTime? FetchedAt { get; set; }
        [Column]
        public int StateId { get; set; }
        [Column]
        public string StateName { get; set; }
        [Column]
        public string StateReason { get; set; }
        [Column]
        public string StateData { get; set; }
    }

}
