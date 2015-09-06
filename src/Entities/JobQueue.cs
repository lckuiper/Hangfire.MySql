using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Mapping;

namespace Hangfire.MySql.src.Entities
{
    [Table]
    internal class JobQueue
    {
        [PrimaryKey]
        public int Id { get; set; }

        [Column]
        public int JobId { get; set; }

        [Column]
        public string Queue { get; set; }

        [Column]
        public DateTime? FetchedAt { get; set; }

        [Column]
        public string FetchToken { get; set; }
    }
}

/**
  
        CREATE TABLE [HangFire].[JobQueue](
            [Id] [int] IDENTITY(1,1) NOT NULL,
            [JobId] [int] NOT NULL,
            [Queue] [nvarchar](20) NOT NULL,
            [FetchedAt] [datetime] NULL,
            
            CONSTRAINT [PK_HangFire_JobQueue] PRIMARY KEY CLUSTERED ([Id] ASC)
        );
        PRINT 'Created table [HangFire].[JobQueue]';
        
        CREATE NONCLUSTERED INDEX [IX_HangFire_JobQueue_JobIdAndQueue] ON [HangFire].[JobQueue] (
            [JobId] ASC,
            [Queue] ASC
        );
        PRINT 'Created index [IX_HangFire_JobQueue_JobIdAndQueue]';
        
        CREATE NONCLUSTERED INDEX [IX_HangFire_JobQueue_QueueAndFetchedAt] ON [HangFire].[JobQueue] (
            [Queue] ASC,
            [FetchedAt] ASC
        );
        PRINT 'Created index [IX_HangFire_JobQueue_QueueAndFetchedAt]';
**/