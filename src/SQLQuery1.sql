 MERGE INTO SkybrudRedirects WITH (HOLDLOCK) AS target
                    USING SkybrudRedirectsImport AS source
                        ON target.LinkUrl = source.LinkUrl
                        AND target.Url = source.Url
                    WHEN MATCHED THEN UPDATE SET 
                    target.QueryString = source.QueryString,
                    target.LinkMode = source.LinkMode,
                    target.LinkId = source.LinkId,
                    target.Created = source.Created,
                    target.Updated = source.Updated,
                    target.IsPermanent = source.IsPermanent,
                    target.IsRegex = source.IsRegex,
                    target.ForwardQueryString = source.ForwardQueryString

                    WHEN NOT MATCHED BY TARGET THEN

                    INSERT(Url, 
                        QueryString, 
                        LinkMode, 
                        LinkId, 
                        LinkUrl, 
                        LinkName, 
                        Created, 
                        Updated, 
                        IsPermanent, 
                        IsRegEx,
                        ForwardQueryString)
                    VALUES(
                        source.Url, 
                        source.QueryString, 
                        source.LinkMode, 
                        source.LinkId, 
                        source.LinkUrl, 
                        source.LinkName, 
                        source.Created, 
                        source.Updated, 
                        source.IsPermanent, 
                        source.IsRegEx,
                        source.ForwardQueryString);