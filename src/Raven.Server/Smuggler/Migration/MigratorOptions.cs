﻿using System;
using System.Net.Http;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Smuggler;
using Raven.Server.Documents;
using Raven.Server.ServerWide;

namespace Raven.Server.Smuggler.Migration
{
    public class MigratorOptions
    {
        public string MigrationStateKey { get; set; }

        public string ServerUrl { get; set; }

        public string DatabaseName { get; set; }

        public HttpClient HttpClient { get; set; }

        public DatabaseItemType OperateOnTypes { get; set; }

        public bool RemoveAnalyzers { get; set; }

        public SmugglerResult Result { get; set; }

        public Action<IOperationProgress> OnProgress { get; set; }

        public DocumentDatabase Database { get; set; }

        public OperationCancelToken CancelToken { get; set; }
    }
}
