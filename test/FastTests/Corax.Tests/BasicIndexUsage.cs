﻿// -----------------------------------------------------------------------
//  <copyright file="BasicIndexUsage.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using Corax.Queries;
using Raven.Server.Json.Parsing;
using Tryouts.Corax.Analyzers;
using Xunit;

namespace Tryouts.Corax.Tests
{
    public class BasicIndexUsage : CoraxTest
    {
        protected override IAnalyzer CreateAnalyzer()
        {
            return new NopAnalyzer();
        }

        [Fact]
        public void CanIndexAndQueryWithBoolean()
        {
            using (var indexer = _fullTextIndex.CreateIndexer())
            {
                indexer.NewEntry(new DynamicJsonValue
                {
                    ["Name"] = "Michael",
                }, "users/2");
                indexer.NewEntry(new DynamicJsonValue
                {
                    ["Name"] = "Arek",
                }, "users/3");
            }

            using (var searcher = _fullTextIndex.CreateSearcher())
            {
                var ids = searcher.Query(new BooleanQuery(QueryOperator.Or,
                    new TermQuery("Name", "Arek"),
                    new TermQuery("Name", "Michael")
                    ), 2);
                Assert.Equal(new[] { "users/2", "users/3" }, ids);
            }
        }

        [Fact]
        public void CanIndexAndQuery()
        {
            using (var indexer = _fullTextIndex.CreateIndexer())
            {
                indexer.NewEntry(new DynamicJsonValue
                {
                    ["Name"] = "Michael",
                }, "users/2");
                indexer.NewEntry(new DynamicJsonValue
                {
                    ["Name"] = "Arek",
                }, "users/3");
            }

            using (var searcher = _fullTextIndex.CreateSearcher())
            {
                var ids = searcher.Query(new TermQuery("Name", "Arek"), 2);
                Assert.Equal(new[] { "users/3" }, ids);
            }
        }
    }
}