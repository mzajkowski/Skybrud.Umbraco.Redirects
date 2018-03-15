using Umbraco.Core.Models;
using Umbraco.Web.PublishedCache;

namespace Skybrud.Umbraco.Redirects.Models.Import
{
    /// <summary>
    /// This class only really exists so I can test the looking up of Umbraco nodes without loads of mocking 
    /// out the Umbraco Context which is a faff
    /// </summary>
    public class RedirectPublishedContentFinder : IRedirectPublishedContentFinder
    {
        private readonly ContextualPublishedContentCache publishedCache;

        public RedirectPublishedContentFinder(ContextualPublishedContentCache publishedCache)
        {
            this.publishedCache = publishedCache;
        }

        public IPublishedContent Find(string url)
        {
            return publishedCache.GetByRoute(false, url, false);
        }
    }
}