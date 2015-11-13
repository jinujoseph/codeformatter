// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;

namespace Microsoft.DotNet.CodeFormatting
{
    internal class AdditionalTextFile : AdditionalText
    {
        private readonly string _path;
        public AdditionalTextFile(string path)
        {
            _path = path;
        }
        public override string Path { get { return _path; } }

        public override SourceText GetText(CancellationToken cancellationToken = default(CancellationToken))
        {
            return SourceText.From(_path);
        }
    }
}
