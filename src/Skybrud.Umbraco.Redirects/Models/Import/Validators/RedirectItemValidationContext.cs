using System.Collections.Generic;

namespace Skybrud.Umbraco.Redirects.Models.Import.Validators
{
    public static class RedirectItemValidationContext
    {
        public static ValidatedRedirectItem Validate(int index, RedirectItem model, IEnumerable<RedirectItem> otherRedirectItems)
        {
            var sourceUrlValidator = new SourceUrlValidator();

            var destinationUrlValidator = new DestinationUrlValidator();
            sourceUrlValidator.SetSuccessor(destinationUrlValidator);

            var redirectChainValidator = new RedirectChainValidator();
            destinationUrlValidator.SetSuccessor(redirectChainValidator);

            var results = sourceUrlValidator.HandleValidation(model, otherRedirectItems);

            var response = new ValidatedRedirectItem { Item = model, ValidationResults = results, Index = index };

            return response;
        }
    }
}