using System;
using Raven.Client.Documents.Queries.Facets;
using Raven.Client.Documents.Session.Tokens;

namespace Raven.Client.Documents.Session
{
    public abstract partial class AbstractDocumentQuery<T, TSelf>
    {
        public void AggregateBy(Facet facet)
        {
            foreach (var token in SelectTokens)
            {
                if (token is FacetToken ft)
                {
                    if (string.Equals(ft.Name, facet.DisplayName, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("TODO ppekrol");
                }
                else
                    throw new InvalidOperationException("TODO ppekrol");
            }

            SelectTokens.AddLast(FacetToken.Create(facet, AddQueryParameter));
        }
    }
}
