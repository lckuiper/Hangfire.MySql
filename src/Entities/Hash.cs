using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;

namespace Hangfire.MySql.src.Entities
{
    [Table]
    internal class Hash
    {
        [PrimaryKey]
        public int Id { get; set; }
        [Column]
        public string Key { get; set; }
        [Column]
        public string Field { get; set; }
        [Column]
        public string Value { get; set; }
        [Column]
        public DateTime? ExpireAt { get; set; }
    }

}

/***
 * CREATE TABLE [HangFire].[Hash](
			[Id] [int] IDENTITY(1,1) NOT NULL,
			[Key] [nvarchar](100) NOT NULL,
			[Field] [nvarchar](100) NOT NULL,
			[Value] [nvarchar](max) NULL,
			[ExpireAt] [datetime2](7) NULL,
		
			CONSTRAINT [PK_HangFire_Hash] PRIMARY KEY CLUSTERED ([Id] ASC)
		);
		PRINT 'Created table [HangFire].[Hash]';

		CREATE UNIQUE NONCLUSTERED INDEX [UX_HangFire_Hash_Key_Field] ON [HangFire].[Hash] (
			[Key] ASC,
			[Field] ASC
		);
 */
