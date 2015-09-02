using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;

namespace Hangfire.MySql.src.Entities
{
    [Table]
    internal class Set
    {
        [PrimaryKey]
        public int Id { get; set; }

        [Column]
        public string Key { get; set; }

        [Column]
        public string Value { get; set; }

        [Column]
        public double Score { get; set; }

        [Column]
        public DateTime? ExpireAt { get; set; }


    }
    /*
     
     *    CREATE TABLE [HangFire].[Set](
            [Id] [int] IDENTITY(1,1) NOT NULL,
            [Key] [nvarchar](100) NOT NULL,
            [Score] [float] NOT NULL,
            [Value] [nvarchar](256) NOT NULL,
            [ExpireAt] [datetime] NULL,
            
            CONSTRAINT [PK_HangFire_Set] PRIMARY KEY CLUSTERED ([Id] ASC)
        );
        PRINT 'Created table [HangFire].[Set]';
        
        CREATE UNIQUE NONCLUSTERED INDEX [UX_HangFire_Set_KeyAndValue] ON [HangFire].[Set] (
            [Key] ASC,
            [Value] ASC
        );
        PRINT 'Created index [UX_HangFire_Set_KeyAndValue]';
     * 
     */
}
