﻿// -----------------------------------------------------------------------
//  <copyright file="IndexRecovery.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------
using System;
using System.IO;
using System.Linq;
using Raven.Client.Document;
using Raven.Database.Config;
using Raven.Database.Extensions;
using Xunit;

namespace Raven.Tests.Indexes.Recovery
{
	public class MapIndexRecoveryTests : RavenTest
	{
		private void CommitPointAfterEachCommitOnly(InMemoryRavenConfiguration configuration)
		{
			// force commit point creation after each commit
			configuration.MinIndexingTimeIntervalToStoreCommitPoint = TimeSpan.FromSeconds(0);
			configuration.MaxIndexCommitPointStoreTimeInterval = TimeSpan.FromSeconds(0);
		}

		private void CommitPointAfterFirstCommitOnly(InMemoryRavenConfiguration configuration)
		{
			// by default first commit will force creating commit point, here we don't need more
			configuration.MinIndexingTimeIntervalToStoreCommitPoint = TimeSpan.FromMinutes(30);
			configuration.MaxIndexCommitPointStoreTimeInterval = TimeSpan.MaxValue;
		}

		[Fact]
		public void ShouldCreateCommitPointsForMapIndexes()
		{
			var index = new MapRecoveryTestIndex();

			using (var server = GetNewServer(runInMemory: false, dataDirectory: DataDir))
			{
				CommitPointAfterEachCommitOnly(server.Database.Configuration);

				using (var store = new DocumentStore { Url = "http://localhost:8079" }.Initialize())
				{
					index.Execute(store);

					using (var session = store.OpenSession())
					{
						session.Store(new Recovery
						{
							Name = "One",
							Number = 1
						});

						session.SaveChanges();
						WaitForIndexing(store);

						session.Store(new Recovery
						{
							Name = "Two",
							Number = 2
						});

						session.SaveChanges();
						WaitForIndexing(store);
					}
				}

				var commitPointsDirectory = Path.Combine(server.Database.Configuration.IndexStoragePath,
														 MonoHttpUtility.UrlEncode(index.IndexName) + "\\CommitPoints");

				Assert.True(Directory.Exists(commitPointsDirectory));

				var commitPoints = Directory.GetDirectories(commitPointsDirectory);

				Assert.Equal(2, commitPoints.Length);

				foreach (var commitPoint in commitPoints)
				{
					var files = Directory.GetFiles(commitPoint);

					Assert.Equal(2, files.Length);

					Assert.Equal("index.commitPoint", Path.GetFileName(files[0]));
					Assert.True(Path.GetFileName(files[1]).StartsWith("segments_"));
				}
			}
		}

		[Fact]
		public void ShouldKeepLimitedNumberOfCommitPoints()
		{
			var index = new MapRecoveryTestIndex();

			using (var server = GetNewServer(runInMemory: false, dataDirectory: DataDir))
			{
				CommitPointAfterEachCommitOnly(server.Database.Configuration);

				var maxNumberOfStoredCommitPoints = server.Database.Configuration.MaxNumberOfStoredCommitPoints;

				using (var store = new DocumentStore { Url = "http://localhost:8079" }.Initialize())
				{
					index.Execute(store);

					for (int i = 0; i < 2 * maxNumberOfStoredCommitPoints; i++)
					{
						using (var session = store.OpenSession())
						{
							session.Store(new Recovery
							{
								Name = i.ToString(),
								Number = i
							});

							session.SaveChanges();
							WaitForIndexing(store);
						}

						var commitPointsDirectory = Path.Combine(server.Database.Configuration.IndexStoragePath,
														 MonoHttpUtility.UrlEncode(index.IndexName) + "\\CommitPoints");

						var commitPoints = Directory.GetDirectories(commitPointsDirectory);

						Assert.True(commitPoints.Length <= maxNumberOfStoredCommitPoints);
					}
				}
			}
		}

		[Fact]
		public void ShouldRecoverMapIndexFromLastCommitPoint()
		{
			string indexFullPath;
			string commitPointsDirectory;
			var index = new MapRecoveryTestIndex();

			using (var server = GetNewServer(runInMemory: false, dataDirectory: DataDir))
			{
				CommitPointAfterFirstCommitOnly(server.Database.Configuration);

				indexFullPath = Path.Combine(server.Database.Configuration.IndexStoragePath,
											 MonoHttpUtility.UrlEncode(index.IndexName));

				commitPointsDirectory = Path.Combine(server.Database.Configuration.IndexStoragePath,
													 MonoHttpUtility.UrlEncode(index.IndexName) + "\\CommitPoints");

				using (var store = new DocumentStore { Url = "http://localhost:8079" }.Initialize())
				{
					index.Execute(store);

					using (var session = store.OpenSession())
					{
						session.Store(new Recovery // indexing this entity will create commit point
						{
							Name = "One",
							Number = 1
						});

						session.SaveChanges();
						WaitForIndexing(store);

						session.Store(new Recovery // this will not be in commit point
						{
							Name = "Two",
							Number = 2
						});

						session.SaveChanges();
						WaitForIndexing(store);
					}
				}
			}

			// make sure that there is only one commit point - which doesn't have the second entity indexed
			Assert.Equal(1, Directory.GetDirectories(commitPointsDirectory).Length);

			IndexMessing.MessSegmentsFile(indexFullPath);

			using (GetNewServer(runInMemory: false, dataDirectory: DataDir, deleteDirectory: false)) // do not delete previous directory
			{
				using (var store = new DocumentStore { Url = "http://localhost:8079" }.Initialize())
				{
					using (var session = store.OpenSession())
					{
						var result =
							session.Query<Recovery, MapRecoveryTestIndex>().Customize(x => x.WaitForNonStaleResults()).ToList();

						Assert.Equal(2, result.Count);
					}
				}

				// here we should have another commit point because missing document after restore were indexed again
				Assert.Equal(2, Directory.GetDirectories(commitPointsDirectory).Length);
			}
		}

		[Fact]
		public void ShouldRecoverDeletes()
		{
			string indexFullPath;
			string commitPointsDirectory;
			var index = new MapRecoveryTestIndex();

			using (var server = GetNewServer(runInMemory: false, dataDirectory: DataDir))
			{
				CommitPointAfterFirstCommitOnly(server.Database.Configuration);

				indexFullPath = Path.Combine(server.Database.Configuration.IndexStoragePath,
											 MonoHttpUtility.UrlEncode(index.IndexName));

				commitPointsDirectory = Path.Combine(server.Database.Configuration.IndexStoragePath,
													 MonoHttpUtility.UrlEncode(index.IndexName) + "\\CommitPoints");

				using (var store = new DocumentStore { Url = "http://localhost:8079" }.Initialize())
				{
					index.Execute(store);

					using (var session = store.OpenSession())
					{
						session.Store(new Recovery
						{
							Name = "One",
							Number = 1
						});

						session.Store(new Recovery
						{
							Name = "Two",
							Number = 2
						});

						session.Store(new Recovery
						{
							Name = "Three",
							Number = 3
						});

						session.SaveChanges(); // store all items in commit point
						WaitForIndexing(store);

						var itemToDelete = session.Query<Recovery, MapRecoveryTestIndex>().First();

						session.Delete(itemToDelete);
						session.SaveChanges();
						WaitForIndexing(store);
					}
				}
			}

			// make sure that there is only one commit point - which doesn't have the second entity indexed
			Assert.Equal(1, Directory.GetDirectories(commitPointsDirectory).Length);

			IndexMessing.MessSegmentsFile(indexFullPath);

			using (GetNewServer(runInMemory: false, dataDirectory: DataDir, deleteDirectory: false)) // do not delete previous directory
			{
				using (var store = new DocumentStore {Url = "http://localhost:8079"}.Initialize())
				{
					using (var session = store.OpenSession())
					{
						var result =
							session.Query<Recovery, MapRecoveryTestIndex>().ToArray();

						Assert.Equal(2, result.Length);
					}
				}
			}
		}
	}
}