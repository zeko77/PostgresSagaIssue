using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using NServiceBus;

namespace Billing
{    
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "Billing";
            await CreateHostBuilder(args).RunConsoleAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                 .ConfigureHostConfiguration(configHost =>
                 {
                     configHost.SetBasePath(Directory.GetCurrentDirectory());
                     configHost.AddJsonFile("appsettings.json", optional: false);
                     configHost.AddCommandLine(args);
                 })
                       .UseNServiceBus(context =>
                       {
                           var endpointConfiguration = new EndpointConfiguration("Billing");
                           var serializationSettings = new JsonSerializerSettings
                           {
                               TypeNameHandling = TypeNameHandling.All,
                               MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
                           };

                           var serialization = endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>(new NewtonsoftJsonSerializer());
                           serialization.Settings(serializationSettings);

                           var recoverability = endpointConfiguration.Recoverability();
                           recoverability.Delayed(d => d.NumberOfRetries(0));
                           recoverability.Immediate(immediate => { immediate.NumberOfRetries(0); });


                           InitializeTransport(context.Configuration, endpointConfiguration);
                           //InitializePostgreSQLPersistence(context.Configuration, endpointConfiguration, serializationSettings);
                           InitializeMySQLPersistence(context.Configuration, endpointConfiguration, serializationSettings);

                           endpointConfiguration.SendFailedMessagesTo("error");
                           endpointConfiguration.AuditProcessedMessagesTo("audit");
                           endpointConfiguration.EnableInstallers();

                           return endpointConfiguration;
                       });
        }


        static void InitializeTransport(IConfiguration config, EndpointConfiguration endpointConfiguration)
        {
            var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
            transport.UseConventionalRoutingTopology(QueueType.Classic);
            transport.ConnectionString(config["RabbitMQ:ConnectionString"]);
            SetupRouting(transport, config);
           
        }

        static void SetupRouting(TransportExtensions<RabbitMQTransport> transport, IConfiguration config)
        {
            var routing = transport.Routing();
          
            routing.RouteToEndpoint(typeof(StartDemoSaga), "Billing");
            routing.RouteToEndpoint(typeof(ExecuteStepOne), "Billing");
            routing.RouteToEndpoint(typeof(ExecuteStepTwo), "Billing");
            routing.RouteToEndpoint(typeof(LastStep), "Billing");
        }

        static void InitializePostgreSQLPersistence(IConfiguration config, EndpointConfiguration endpointConfiguration, JsonSerializerSettings serializationSettings)
        {
            var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
            var sagaSettings = persistence.SagaSettings();
            sagaSettings.JsonSettings(serializationSettings);
            persistence.TablePrefix("TST");

            var dialect = persistence.SqlDialect<SqlDialect.PostgreSql>();
            dialect.JsonBParameterModifier(
                modifier: parameter =>
                {
                    var npgsqlParameter = (NpgsqlParameter)parameter;
                    npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Jsonb;
                });
            persistence.ConnectionBuilder(
                connectionBuilder: () => new NpgsqlConnection(config["PostgreSQL:ConnectionString"]));


            var subscriptions = persistence.SubscriptionSettings();
            subscriptions.CacheFor(TimeSpan.FromMinutes(1));

        }

        static void InitializeMySQLPersistence(IConfiguration config, EndpointConfiguration endpointConfiguration, JsonSerializerSettings serializationSettings)
        {
            var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
            var subscriptions = persistence.SubscriptionSettings();
            subscriptions.CacheFor(TimeSpan.FromMinutes(1));
            persistence.SqlDialect<SqlDialect.MySql>();
            persistence.ConnectionBuilder(
                connectionBuilder: () =>
                {
                    return new MySqlConnection(config["MySQL:ConnectionString"]);
                });
        }
    }
}
