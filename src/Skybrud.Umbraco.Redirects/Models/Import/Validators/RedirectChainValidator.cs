using System.Collections.Generic;
using System.Linq;

namespace Skybrud.Umbraco.Redirects.Models.Import.Validators
{
    public class RedirectChainValidator : ValidatorBase
    {
        public override List<RedirectItemValidationResult> HandleValidation(RedirectItem redirectItem, IEnumerable<RedirectItem> otherRedirectItems)
        {
            var response = new RedirectItemValidationResult();

            var destinationRedirect = otherRedirectItems.FirstOrDefault(item => item.Url == redirectItem.LinkUrl);

            if (destinationRedirect != null)
            {
                if (destinationRedirect.LinkUrl == redirectItem.Url)
                {
                    response.Status = ImportErrorLevel.Error;
                    response.ErrorMessage = string.Format("This redirect would create a redirect loop as another redirect exists with the URL ({0}) in the file. It has not been imported.", redirectItem.LinkUrl);

                    ErrorsResult.Add(response);

                    return ErrorsResult;
                }
                else
                {
                    response.Status = ImportErrorLevel.Warning;
                    response.ErrorMessage = string.Format("This redirect links to the URL ({0}) in the file. This will result in a redirect chain", redirectItem.LinkUrl);

                    ErrorsResult.Add(response);

                    return ErrorsResult;
                }
            }

            if (Successor != null)
            {
                return Successor.HandleValidation(redirectItem, otherRedirectItems);
            }

            ErrorsResult.Add(response);

            return ErrorsResult;
        }
    }
}