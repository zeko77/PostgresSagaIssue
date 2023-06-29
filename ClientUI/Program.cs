using System;
using System.IO;
using Messages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;
using NServiceBus;

namespace ClientUI
{   
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "ClientUI";
            CreateHostBuilder(args).Build().Run();
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
                            var endpointConfiguration = new EndpointConfiguration("ClientUI");


                            var serializationSettings = new JsonSerializerSettings
                            {
                                TypeNameHandling = TypeNameHandling.All,
                                MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
                            };

                            var serialization = endpointConfiguration.UseSerialization<NewtonsoftJsonSerializer>(new NewtonsoftJsonSerializer());
                            serialization.Settings(serializationSettings);


                            InitializeTransport(context.Configuration, endpointConfiguration);
                            InitializePostgreSQLPersistence(context.Configuration, endpointConfiguration, serializationSettings);

                            endpointConfiguration.SendFailedMessagesTo("error");
                            endpointConfiguration.AuditProcessedMessagesTo("audit");



                            endpointConfiguration.EnableInstallers();
                            return endpointConfiguration;

                        })
                       .ConfigureWebHostDefaults(webBuilder =>
                        {
                            webBuilder.UseStartup<Startup>();
                        });

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

                //routing.RouteToEndpoint(typeof(PlaceOrder), "Sales");
                routing.RouteToEndpoint(typeof(StartDemoSaga), "Billing");
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
                    connectionBuilder: () => {
                        var cnn = new NpgsqlConnection(config["PostgreSQL:ConnectionString"]);
                        return cnn;
                    }
                    );


                var subscriptions = persistence.SubscriptionSettings();
                subscriptions.CacheFor(TimeSpan.FromMinutes(1));

            }
        }
    }
}
