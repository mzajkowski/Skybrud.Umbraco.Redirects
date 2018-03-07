using System.Collections.Generic;
using System.Linq;
using Skybrud.Umbraco.Redirects.Import.Csv;

namespace Skybrud.Umbraco.Redirects.Models.Import
{
    public class ImporterResponse
    {
        public ImporterResponse()
        {
            ImportedItems = Enumerable.Empty<ValidatedRedirectItem>();
        }

        public CsvFile File { get; set; }

        public IEnumerable<ValidatedRedirectItem> ImportedItems { get; set;  }
    }
}