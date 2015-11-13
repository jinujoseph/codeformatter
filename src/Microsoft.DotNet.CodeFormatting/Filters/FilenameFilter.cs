// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;


namespace Microsoft.DotNet.CodeFormatting.Filters
{
    internal sealed class FilenameFilter : IFormattingFilter
    {
        private readonly FormattingOptions _options;

        public FilenameFilter(FormattingOptions options)
        {
            _options = options;
        }

        public bool ShouldBeProcessed(Document document)
        {
            System.Collections.Immutable.ImmutableArray<string> fileNames = _options.FileNames;
            if (fileNames.IsDefaultOrEmpty)
            {
                return true;
            }

            string docFilename = Path.GetFileName(document.FilePath);
            foreach (string filename in fileNames)
            {
                if (filename.Equals(docFilename, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
