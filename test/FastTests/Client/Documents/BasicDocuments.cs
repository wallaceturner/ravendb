﻿using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Raven.Client.Documents.Commands;
using Raven.Client.Documents.Session;
using Raven.Tests.Core.Utils.Entities;
using Sparrow.Json;
using Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace FastTests.Client.Documents
{
    public class BasicDocuments : RavenTestBase
    {
        public BasicDocuments(ITestOutputHelper output) : base(output)
        {
        }

        [RavenTheory(RavenTestCategory.ClientApi)]
        [RavenData(DatabaseMode = RavenDatabaseMode.All)]
        public async Task CanStoreAnonymousObject(Options options)
        {
            using (var store = GetDocumentStore(options))
            {
                using (var session = store.OpenAsyncSession())
                {
                    await session.StoreAsync(new { Name = "Fitzchak" });
                    await session.StoreAsync(new { Name = "Arek" });

                    await session.SaveChangesAsync();
                }
            }
        }

        [RavenTheory(RavenTestCategory.ClientApi)]
        [RavenData(DatabaseMode = RavenDatabaseMode.All)]
        public async Task CanChangeDocumentCollectionWithDeleteAndSave(Options options)
        {
            using (var store = GetDocumentStore(options))
            {
                const string documentId = "users/1";
                using (var session = store.OpenAsyncSession())
                {
                    await session.StoreAsync(new User { Name = "Grisha" }, documentId);
                    await session.SaveChangesAsync();
                }

                using (var session = store.OpenAsyncSession())
                {
                    session.Delete(documentId);
                    await session.SaveChangesAsync();
                }

                using (var session = store.OpenAsyncSession())
                {
                    await session.StoreAsync(new Person { Name = "Grisha" }, documentId);
                    await session.SaveChangesAsync();
                }
            }
        }

        [RavenTheory(RavenTestCategory.ClientApi)]
        [RavenData(DatabaseMode = RavenDatabaseMode.All)]
        public async Task GetAsync(Options options)
        {
            using (var store = GetDocumentStore(options))
            {
                var dummy = JObject.FromObject(new User());
                dummy.Remove("Id");

                using (var session = store.OpenAsyncSession())
                {
                    await session.StoreAsync(new User { Name = "Fitzchak" }, "users/1");
                    await session.StoreAsync(new User { Name = "Arek" }, "users/2");

                    await session.SaveChangesAsync();
                }

                var requestExecuter = store
                    .GetRequestExecutor();

                using (requestExecuter.ContextPool.AllocateOperationContext(out JsonOperationContext context))
                {
                    var getDocumentCommand = new GetDocumentsCommand(store.Conventions, new[] { "users/1", "users/2" }, includes: null, metadataOnly: false);

                    requestExecuter
                        .Execute(getDocumentCommand, context);

                    var docs = getDocumentCommand.Result;
                    Assert.Equal(2, docs.Results.Length);

                    var doc1 = docs.Results[0] as BlittableJsonReaderObject;
                    var doc2 = docs.Results[1] as BlittableJsonReaderObject;

                    Assert.NotNull(doc1);

                    var doc1Properties = doc1.GetPropertyNames();
                    Assert.True(doc1Properties.Contains("@metadata"));
                    Assert.Equal(dummy.Count + 1, doc1Properties.Length); // +1 for @metadata

                    Assert.NotNull(doc2);

                    var doc2Properties = doc2.GetPropertyNames();
                    Assert.True(doc2Properties.Contains("@metadata"));
                    Assert.Equal(dummy.Count + 1, doc2Properties.Length); // +1 for @metadata

                    using (var session = (DocumentSession)store.OpenSession())
                    {
                        var user1 = (User)session.JsonConverter.FromBlittable(typeof(User), ref doc1, "users/1", trackEntity: true);
                        var user2 = (User)session.JsonConverter.FromBlittable(typeof(User), ref doc2, "users/2", trackEntity: true);

                        Assert.Equal("Fitzchak", user1.Name);
                        Assert.Equal("Arek", user2.Name);
                    }

                    getDocumentCommand = new GetDocumentsCommand(store.Conventions, new[] { "users/1", "users/2" }, includes: null,
                        metadataOnly: true);

                    requestExecuter
                        .Execute(getDocumentCommand, context);

                    docs = getDocumentCommand.Result;
                    Assert.Equal(2, docs.Results.Length);

                    doc1 = docs.Results[0] as BlittableJsonReaderObject;
                    doc2 = docs.Results[1] as BlittableJsonReaderObject;

                    Assert.NotNull(doc1);

                    doc1Properties = doc1.GetPropertyNames();
                    Assert.True(doc1Properties.Contains("@metadata"));
                    Assert.Equal(1, doc1Properties.Length);

                    Assert.NotNull(doc2);

                    doc2Properties = doc2.GetPropertyNames();
                    Assert.True(doc2Properties.Contains("@metadata"));
                    Assert.Equal(1, doc2Properties.Length);
                }
            }
        }
    }
}
