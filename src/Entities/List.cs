using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;

namespace Hangfire.MySql.src.Entities
{
    [Table]
    public class List
    {
        [PrimaryKey]
        public int Id { get; set; }

        [Column]
        public string Key { get; set; }

        [Column]
        public string Value { get; set; }

        [Column]
        public DateTime? ExpireAt { get; set; }

    }

    /** CREATE TABLE [HangFire].[List](
            [Id] [int] IDENTITY(1,1) NOT NULL,
            [Key] [nvarchar](100) NOT NULL,
            [Value] [nvarchar](max) NULL,
            [ExpireAt] [datetime] NULL,
     * 
     * */
}
