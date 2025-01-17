﻿using System.Collections.Generic;
using System.Linq;
using FastTests;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace SlowTests.Issues
{
    public class RavenDB741 : RavenTestBase
    {
        public RavenDB741(ITestOutputHelper output) : base(output)
        {
        }

        [RavenTheory(RavenTestCategory.Indexes | RavenTestCategory.Querying)]
        [RavenData(SearchEngineMode = RavenSearchEngineMode.All, DatabaseMode = RavenDatabaseMode.All)]
        public void Nested_Dictionary_Dynamic_Count_Property_Should_Work(Options options)
        {
            using (var documentStore = GetDocumentStore(options))
            {
                documentStore.Initialize();

                PutSampleData(documentStore);

                using (var session = documentStore.OpenSession())
                {
                    var results = session.Query<Foo>()
                                         .Customize(x => x.WaitForNonStaleResults())
                                         .Where(x => x.Bars.Count == 0)
                                         .ToList();

                    Assert.Equal(2, results.Count);
                }
            }
        }

        [RavenTheory(RavenTestCategory.Indexes | RavenTestCategory.Querying)]
        [RavenData(SearchEngineMode = RavenSearchEngineMode.All, DatabaseMode = RavenDatabaseMode.All)]
        public void Nested_Dictionary_Dynamic_Count_Method_Should_Work(Options options)
        {
            using (var documentStore = GetDocumentStore(options))
            {
                documentStore.Initialize();

                PutSampleData(documentStore);

                using (var session = documentStore.OpenSession())
                {
                    var results = session.Query<Foo>()
                                         .Customize(x => x.WaitForNonStaleResults())
                                         .Where(x => x.Bars.Count() == 0)
                                         .ToList();

                    Assert.Equal(2, results.Count);
                }
            }
        }

        [Fact]
        public void Nested_Dictionary_Dynamic_Enumerable_Count_Should_Work()
        {
            using (var documentStore = GetDocumentStore())
            {
                documentStore.Initialize();

                PutSampleData(documentStore);

                using (var session = documentStore.OpenSession())
                {
                    var results = session.Query<Foo>()
                                         .Customize(x => x.WaitForNonStaleResults())
                                         .Where(x => Enumerable.Count(x.Bars) == 0)
                                         .ToList();

                    WaitForUserToContinueTheTest(documentStore);

                    Assert.Equal(2, results.Count);
                }
            }
        }

        [Fact]
        public void Nested_Dictionary_Static_Count_Property_Should_Work()
        {
            using (var documentStore = GetDocumentStore())
            {
                documentStore.Initialize();
                documentStore.ExecuteIndex(new Foos_ByBarCount_Property());

                PutSampleData(documentStore);

                using (var session = documentStore.OpenSession())
                {
                    var results = session.Query<Foo, Foos_ByBarCount_Property>()
                                         .Customize(x => x.WaitForNonStaleResults())
                                         .Where(x => x.Bars.Count == 0)
                                         .ToList();

                    Assert.Equal(2, results.Count);
                }
            }
        }

        [Fact]
        public void Nested_Dictionary_Static_Count_Method_Should_Work()
        {
            using (var documentStore = GetDocumentStore())
            {
                documentStore.Initialize();
                documentStore.ExecuteIndex(new Foos_ByBarCount_Method());

                PutSampleData(documentStore);

                using (var session = documentStore.OpenSession())
                {
                    var results = session.Query<Foo, Foos_ByBarCount_Method>()
                                         .Customize(x => x.WaitForNonStaleResults())
                                         .Where(x => x.Bars.Count == 0)
                                         .ToList();

                    Assert.Equal(2, results.Count);
                }
            }
        }

        [Fact]
        public void Nested_Dictionary_Static_Enumerable_Count_Should_Work()
        {
            using (var documentStore = GetDocumentStore())
            {
                documentStore.Initialize();
                documentStore.ExecuteIndex(new Foos_ByBarCount_Enumerable());

                PutSampleData(documentStore);

                using (var session = documentStore.OpenSession())
                {
                    var results = session.Query<Foo, Foos_ByBarCount_Enumerable>()
                                         .Customize(x => x.WaitForNonStaleResults())
                                         .Where(x => x.Bars.Count == 0)
                                         .ToList();

                    Assert.Equal(2, results.Count);
                }
            }
        }

        private static void PutSampleData(IDocumentStore documentStore)
        {
            using (var session = documentStore.OpenSession())
            {
                session.Store(new Foo
                {
                    Id = "foos/1",
                    Bars = new Dictionary<string, Bar>
                            {
                                { "A", new Bar { Name = "A", Value = "1" } },
                                { "B", new Bar { Name = "B", Value = "2" } },
                                { "C", new Bar { Name = "C", Value = "3" } },
                            }
                });

                session.Store(new Foo
                {
                    Id = "foos/2",
                    Bars = new Dictionary<string, Bar>
                            {
                                { "D", new Bar { Name = "D", Value = "4" } },
                                { "E", new Bar { Name = "E", Value = "5" } },
                                { "F", new Bar { Name = "F", Value = "6" } },
                            }
                });

                session.Store(new Foo
                {
                    Id = "foos/3",
                    Bars = new Dictionary<string, Bar>()
                });

                session.Store(new Foo
                {
                    Id = "foos/4",
                    Bars = new Dictionary<string, Bar>()
                });

                session.SaveChanges();
            }
        }

        private class Foo
        {
            public string Id { get; set; }
            public Dictionary<string, Bar> Bars { get; set; }
        }

        private class Bar
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        private class Foos_ByBarCount_Property : AbstractIndexCreationTask<Foo>
        {
            public Foos_ByBarCount_Property()
            {
                Map = foos => from foo in foos
                              select new
                              {
                                  Bars_Count = foo.Bars.Count
                              };
            }
        }

        private class Foos_ByBarCount_Method : AbstractIndexCreationTask<Foo>
        {
            public Foos_ByBarCount_Method()
            {
                Map = foos => from foo in foos
                              select new
                              {
                                  Bars_Count = foo.Bars.Count()
                              };
            }
        }

        private class Foos_ByBarCount_Enumerable : AbstractIndexCreationTask<Foo>
        {
            public Foos_ByBarCount_Enumerable()
            {
                Map = foos => from foo in foos
                              select new
                              {
                                  Bars_Count = Enumerable.Count(foo.Bars)
                              };
            }
        }
    }
}
