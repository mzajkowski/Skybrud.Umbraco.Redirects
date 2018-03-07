using Umbraco.Core.Models;
using Umbraco.Web.PublishedCache;

namespace Skybrud.Umbraco.Redirects.Models.Import
{
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