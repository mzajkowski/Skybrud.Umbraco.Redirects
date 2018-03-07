using Umbraco.Core.Models;

namespace Skybrud.Umbraco.Redirects.Models.Import
{
    public interface IRedirectPublishedContentFinder
    {
        IPublishedContent Find(string url);
    }
}