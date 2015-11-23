// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.Composition.Hosting;
using System.Collections.Generic;

namespace Microsoft.DotNet.CodeFormatting
{
    public static class FormattingEngine
    {
        public static IFormattingEngine Create()
        {
            CompositionContainer container = CreateCompositionContainer();
            IFormattingEngine engine = container.GetExportedValue<IFormattingEngine>();
            var consoleFormatLogger = new ConsoleFormatLogger();
            return engine;
        }

        public static List<IRuleMetadata> GetFormattingRules()
        {
            CompositionContainer container = CreateCompositionContainer();
            var list = new List<IRuleMetadata>();
            AppendRules<ISyntaxFormattingRule>(list, container);
            AppendRules<ILocalSemanticFormattingRule>(list, container);
            AppendRules<IGlobalSemanticFormattingRule>(list, container);
            return list;
        }

        private static void AppendRules<T>(List<IRuleMetadata> list, CompositionContainer container)
            where T : IFormattingRule
        {
            foreach (Lazy<T, IRuleMetadata> rule in container.GetExports<T, IRuleMetadata>())
            {
                list.Add(rule.Metadata);
            }
        }

        private static CompositionContainer CreateCompositionContainer()
        {
            var catalog = new AssemblyCatalog(typeof(FormattingEngine).Assembly);
            return new CompositionContainer(catalog);
        }
    }
}