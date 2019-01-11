using Microsoft.Extensions.Logging;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.TestingHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TestExtensions;
using UnitTests.GrainInterfaces;
using UnitTests.Grains;

namespace Orleans.Indexing.Tests
{
    public abstract class BaseIndexingFixture : BaseTestClusterFixture
    {
        protected TestClusterBuilder ConfigureTestClusterForIndexing(TestClusterBuilder builder)
        {
            // Currently nothing
            //builder.Options.InitialSilosCount = 1;    // For debugging if needed
            return builder;
        }

        internal static ISiloHostBuilder Configure(ISiloHostBuilder hostBuilder, string databaseName = null)
        {
            if (!TestDefaultConfiguration.GetValue("CosmosDBEndpoint", out string cosmosDBEndpoint)
                || !TestDefaultConfiguration.GetValue("CosmosDBKey", out string cosmosDBKey))
            {
                throw new IndexConfigurationException("CosmosDB connection values are not specified");
            }

            hostBuilder.AddMemoryGrainStorage(IndexingTestConstants.GrainStore)
                       .AddMemoryGrainStorage("PubSubStore") // PubSubStore service is needed for the streams underlying OrleansQueryResults
                       .ConfigureLogging(loggingBuilder =>
                       {
                           loggingBuilder.SetMinimumLevel(LogLevel.Information);
                           loggingBuilder.AddDebug();
                       })
                       .ConfigureApplicationParts(parts =>
                       {
                           parts.AddApplicationPart(typeof(BaseIndexingFixture).Assembly);
                           parts.AddApplicationPart(typeof(ISimpleGrain).Assembly);
                           parts.AddApplicationPart(typeof(SimpleGrain).Assembly);
                       });

            return databaseName != null
                ? hostBuilder.AddCosmosDBGrainStorage(IndexingTestConstants.CosmosDBGrainStorage, opt =>
                    {
                        opt.AccountEndpoint = cosmosDBEndpoint;
                        opt.AccountKey = cosmosDBKey;
                        opt.ConnectionMode = Microsoft.Azure.Documents.Client.ConnectionMode.Gateway;
                        opt.DropDatabaseOnInit = true;
                        opt.AutoUpdateStoredProcedures = true;
                        opt.CanCreateResources = true;
                        opt.DB = databaseName;
                        opt.InitStage = ServiceLifecycleStage.RuntimeStorageServices;
                        opt.StateFieldsToIndex.AddRange(GetStateFieldsToIndex());
                    })
                : hostBuilder;
        }

        internal static IClientBuilder Configure(IClientBuilder clientBuilder)
        {
            return clientBuilder.ConfigureLogging(loggingBuilder =>
                                {
                                    loggingBuilder.SetMinimumLevel(LogLevel.Information);
                                    loggingBuilder.AddDebug();
                                })
                                .ConfigureApplicationParts(parts =>
                                {
                                    parts.AddApplicationPart(typeof(BaseIndexingFixture).Assembly);
                                    parts.AddApplicationPart(typeof(ISimpleGrain).Assembly);
                                    parts.AddApplicationPart(typeof(SimpleGrain).Assembly);
                                });
        }

        // Code below adapted from AppPartsIndexableGrainLoader to identify the necessary fields for the DSMI storage
        // provider to index.

        private static IEnumerable<string> GetStateFieldsToIndex()
        {
            var grainClassTypes = typeof(BaseIndexingFixture).Assembly.GetConcreteGrainClasses(logger: null).ToArray();

            // Orleans.CosmosDB appends the field names to "State."; thus we do not prepend the interface names.
            var interfacesToIndexedPropertyNames = new Dictionary<Type, string[]>();
            foreach (var grainClassType in grainClassTypes)
            {
                GetFieldsForASingleGrainType(grainClassType, interfacesToIndexedPropertyNames);
            }
            return new HashSet<string>(interfacesToIndexedPropertyNames.Where(kvp => kvp.Value.Length > 0).SelectMany(kvp => kvp.Value));
        }

        private static void GetFieldsForASingleGrainType(Type grainClassType, Dictionary<Type, string[]> interfacesToIndexedPropertyNames)
        {
            Type[] allInterfaces = grainClassType.GetInterfaces();

            // If there are any interface that directly extends IIndexableGrain<TProperties>...
            var indexableBaseInterfaces = allInterfaces.Where(itf => itf.IsGenericType && itf.GetGenericTypeDefinition() == typeof(IIndexableGrain<>)).ToArray();
            if (indexableBaseInterfaces.Length == 0)
            {
                return;
            }

            var fieldPrefix = grainClassType.IsFaultTolerant() ? "UserState." : string.Empty;
            foreach (var indexableBaseInterface in indexableBaseInterfaces)
            {
                // ... and its generic argument is a class (TProperties)... 
                var propertiesClassType = indexableBaseInterface.GetGenericArguments()[0];
                if (propertiesClassType.GetTypeInfo().IsClass)
                {
                    // ... then examine all indexed fields for all the descendant interfaces of IIndexableGrain<TProperties>; these interfaces are defined by end-users.
                    foreach (var grainInterfaceType in allInterfaces.Where(itf => !indexableBaseInterfaces.Contains(itf)
                                                                                    && indexableBaseInterface.IsAssignableFrom(itf)
                                                                                    && !interfacesToIndexedPropertyNames.ContainsKey(itf)))
                    {
                        interfacesToIndexedPropertyNames[grainInterfaceType] = propertiesClassType.GetProperties()
                                                                                .Where(propInfo => propInfo.GetCustomAttributes<StorageManagedIndexAttribute>(inherit: false).Any())
                                                                                .Select(propInfo => fieldPrefix + propInfo.Name)
                                                                                .ToArray();
                    }
                }
            }
        }
    }
}
