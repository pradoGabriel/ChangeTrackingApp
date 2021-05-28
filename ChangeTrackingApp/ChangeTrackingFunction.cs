using ChangeTrackingApp.Dal.Database;
using ChangeTrackingApp.Dal.Database.Models;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.DynamicLinq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChangeTrackingApp
{
    public class ChangeTrackingFunction
    {
        private readonly ApiContext _context;
        private readonly IConfiguration _config;
        private readonly string connectionString;

        public ChangeTrackingFunction(ApiContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            connectionString = _config.GetValue<string>("ServiceBusConnectionString");
        }

        [FunctionName("ChangeTrackingTest")]
        public async Task Run(
            [TimerTrigger("5-7 * * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation("C# Timer trigger function processed a request.");

            //Apenas gerando massa de dados para os testes
            await Populate();

            var storageConnectionString = "Connection string here";
            var azureTableName = "ChangeTrackingVersion";
            var sqlTableName = "ChangeTracking";

            CloudStorageAccount storageAccount;
            storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            CloudTable table = tableClient.GetTableReference(azureTableName);

            var operation = TableOperation.Retrieve<ChangeTrackingVersionModel>(sqlTableName, "0");
            var result = table.Execute(operation);
            var startVersion = result.Result as ChangeTrackingVersionModel;

            var endVersion = RawSqlQuery("SELECT CHANGE_TRACKING_CURRENT_VERSION()", x => new ChangeTrackingVersionModel { Version = (long)x[0] });
            endVersion.PartitionKey = sqlTableName;
            endVersion.RowKey = "0";

            var endVersionID = new SqlParameter("endVersionId", endVersion.Version);
            var startVersionID = new SqlParameter("startVersionId", startVersion.Version);

            var updates = await _context.ChangeTracking
                .FromSqlRaw(@"SELECT 
                                CT.*
                                FROM CHANGETABLE(CHANGES dbo.ChangeTracking, @startVersionID) C
                                LEFT JOIN dbo.ChangeTracking CT
                                ON C.ID = CT.ID
                                WHERE (SELECT MAX(v)
                                FROM (VALUES(C.SYS_CHANGE_VERSION), (C.SYS_CHANGE_CREATION_VERSION)) AS VALUE(v)) <= @endVersionID",
                                startVersionID, endVersionID)
                .ToListAsync();

            await MergeVersion(table, endVersion);
            await SendMessageToTopic(updates);
        }

        private async Task SendMessageToTopic(List<ChangeTrackingModel> alteracoes)
        {
            var topicName = "topic-db-sync";

            var client = new TopicClient(connectionString, topicName);
            string messageBody = JsonSerializer.Serialize(alteracoes);
            var message = new Message(Encoding.UTF8.GetBytes(messageBody));

            await client.SendAsync(message);
            await client.CloseAsync();
        }

        private async Task Populate()
        {

            for (var a = 1; a <= 4; a++)
            {
                var test = new ChangeTrackingModel()
                {
                    RandomString = Guid.NewGuid().ToString()
                };

                await _context.ChangeTracking.AddAsync(test);
            }

            await _context.SaveChangesAsync();
            // Editando primeiro item
            var first = await _context.ChangeTracking.FirstOrDefaultAsync();
            first.RandomString = Guid.NewGuid().ToString();
            _context.Entry(first).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        private T RawSqlQuery<T>(string query, Func<DbDataReader, T> map)
        {
            using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = query;
            command.CommandType = CommandType.Text;

            _context.Database.OpenConnection();

            using var result = command.ExecuteReader();
            var entities = new List<T>();

            while (result.Read())
            {
                entities.Add(map(result));
            }

            return entities[0];
        }

        private static async Task MergeVersion(CloudTable table, ChangeTrackingVersionModel version)
        {
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(version);

            TableResult result = await table.ExecuteAsync(insertOrMergeOperation);
            var insertedVersion = result.Result as ChangeTrackingVersionModel;

            Console.WriteLine("Atualizado até a versão - {0} - Version: {1}", insertedVersion.PartitionKey, insertedVersion.Version);
        }
    }
}
