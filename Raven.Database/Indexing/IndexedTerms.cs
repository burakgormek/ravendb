﻿// -----------------------------------------------------------------------
//  <copyright file="CachedIndexedTerms.cs" company="Hibernating Rhinos LTD">
//      Copyright (c) Hibernating Rhinos LTD. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Raven.Imports.Newtonsoft.Json.Linq;
using Raven.Json.Linq;

namespace Raven.Database.Indexing
{
	public class IndexedTerms
	{
		public static void ReadEntriesForFields(IndexReader reader, HashSet<string> fieldsToRead, HashSet<int> docIds, Action<Term> onTermFound)
		{
			using (var termEnum = reader.Terms())
			using (var termDocs = reader.TermDocs())
			{
				do
				{
					if (termEnum.Term == null ||
						fieldsToRead.Contains(termEnum.Term.Field) == false)
						continue;

					termDocs.Seek(termEnum.Term);
					for (int i = 0; i < termEnum.DocFreq() && termDocs.Next(); i++)
					{
						if(docIds.Contains(termDocs.Doc) == false)
							continue;
						onTermFound(termEnum.Term);
					}
				} while (termEnum.Next());

			}
		}

		public static RavenJObject[] ReadAllEntriesFromIndex(IndexReader reader)
		{
			if (reader.MaxDoc > 128 * 1024)
			{
				throw new InvalidOperationException("Refusing to extract all index entires from an index with " + reader.MaxDoc +
													" entries, because of the probable time / memory costs associated with that." +
													Environment.NewLine +
													"Viewing Index Entries are a debug tool, and should not be used on indexes of this size. You might want to try Luke, instead.");
			}
			var results = new RavenJObject[reader.MaxDoc];
			using (var termDocs = reader.TermDocs())
			using (var termEnum = reader.Terms())
			{
				while (termEnum.Next())
				{
					var term = termEnum.Term;
					if (term == null)
						break;

					var text = term.Text;

					termDocs.Seek(termEnum);
					for (int i = 0; i < termEnum.DocFreq() && termDocs.Next(); i++)
					{
						RavenJObject result = results[termDocs.Doc];
						if (result == null)
							results[termDocs.Doc] = result = new RavenJObject();
						var propertyName = term.Field;
						if (propertyName.EndsWith("_ConvertToJson") ||
							propertyName.EndsWith("_IsArray"))
							continue;
						if (result.ContainsKey(propertyName))
						{
							switch (result[propertyName].Type)
							{
								case JTokenType.Array:
									((RavenJArray)result[propertyName]).Add(text);
									break;
								case JTokenType.String:
									result[propertyName] = new RavenJArray
									{
										result[propertyName],
										text
									};
									break;
								default:
									throw new ArgumentException("No idea how to handle " + result[propertyName].Type);
							}
						}
						else
						{
							result[propertyName] = text;
						}
					}
				}
			}
			return results;
		}

	}
}