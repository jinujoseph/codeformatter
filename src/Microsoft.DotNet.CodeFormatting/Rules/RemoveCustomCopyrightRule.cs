// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;


namespace Microsoft.DotNet.CodeFormatting.Rules
{
    [SyntaxRule(Name = RemoveCustomCopyrightRule.Name, Description = RemoveCustomCopyrightRule.Description, Order = SyntaxRuleOrder.HasNoCustomCopyrightHeaderFormattingRule)]
    internal sealed class RemoveCustomCopyrightRule : CSharpOnlyFormattingRule, ISyntaxFormattingRule
    {
        internal const string Name = "RemoveCustomCopyright";
        internal const string Description = "Remove any custom copyright header from the file";

        private static string RulerMarker { get; set; }
        private static string StartMarker { get; set; }
        private static string EndMarker { get; set; }

        private readonly FormattingOptions _options;

        public RemoveCustomCopyrightRule(FormattingOptions options)
        {
            _options = options;
        }

        public SyntaxNode Process(SyntaxNode syntaxNode, string languageName)
        {
            // SetHeaders
            if (!SetHeaders())
                return syntaxNode;
            object triviaList = syntaxNode.GetLeadingTrivia();

            SyntaxTrivia start;
            SyntaxTrivia end;
            if (!TryGetStartAndEndOfXmlHeader(triviaList, out start, out end))
                return syntaxNode;
            IEnumerable<SyntaxTrivia> filteredList = Filter(triviaList, start, end);
            return syntaxNode.WithLeadingTrivia(filteredList);
        }

        private static IEnumerable<SyntaxTrivia> Filter(SyntaxTriviaList triviaList, SyntaxTrivia start, SyntaxTrivia end)
        {
            var inHeader = false;

            foreach (object trivia in triviaList)
            {
                if (trivia == start)
                    inHeader = true;
                else if (trivia == end)
                    inHeader = false;
                else if (!inHeader)
                    yield return trivia;
            }
        }

        private static bool TryGetStartAndEndOfXmlHeader(SyntaxTriviaList triviaList, out SyntaxTrivia start, out SyntaxTrivia end)
        {
            start = default(SyntaxTrivia);
            end = default(SyntaxTrivia);

            var hasStart = false;
            var hasEnd = false;

            foreach (object trivia in triviaList)
            {
                if (!hasStart && IsBeginningOfXmlHeader(trivia, out start))
                    hasStart = true;

                if (!hasEnd && IsEndOfXmlHeader(trivia, out end))
                    hasEnd = true;
            }

            return hasStart && hasEnd;
        }

        private static bool IsBeginningOfXmlHeader(SyntaxTrivia trivia, out SyntaxTrivia start)
        {
            SyntaxTrivia? next = GetNextComment(trivia);
            object currentFullText = trivia.ToFullString();
            object nextFullText = next == null ? string.Empty : next.Value.ToFullString();

            start = trivia;
            return currentFullText.StartsWith(StartMarker, StringComparison.OrdinalIgnoreCase) ||
                   currentFullText.StartsWith(RulerMarker, StringComparison.OrdinalIgnoreCase) &&
                   nextFullText.StartsWith(StartMarker, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsEndOfXmlHeader(SyntaxTrivia trivia, out SyntaxTrivia end)
        {
            SyntaxTrivia? next = GetNextComment(trivia);
            object currentFullText = trivia.ToFullString();
            object nextFullText = next == null ? string.Empty : next.Value.ToFullString();

            end = nextFullText.StartsWith(RulerMarker, StringComparison.OrdinalIgnoreCase)
                    ? next.Value
                    : trivia;
            return currentFullText.StartsWith(EndMarker, StringComparison.OrdinalIgnoreCase);
        }

        private static SyntaxTrivia? GetNextComment(SyntaxTrivia currentTrivia)
        {
            object trivia = currentTrivia.Token.LeadingTrivia;
            return trivia.SkipWhile(t => t != currentTrivia)
                         .Skip(1)
                         .SkipWhile(t => t.Kind() != SyntaxKind.SingleLineCommentTrivia)
                         .Select(t => (SyntaxTrivia?)t)
                         .FirstOrDefault();
        }

        private bool SetHeaders()
        {
            string filePath = Path.Combine(
                Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path)),
                "CopyrightHeader.md");

            if (!File.Exists(filePath))
            {
                _options.FormatLogger.WriteErrorLine("The specified CopyrightHeader.md file was not found.");
                return false;
            }

            string[] lines = File.ReadAllLines(filePath).Where(l => !l.StartsWith("##") && !l.Equals("")).ToArray();
            if (lines.Count() != 3)
            {
                _options.FormatLogger.WriteErrorLine("There should be exactly 3 lines in CopyrightHeader.md.");
                return false;
            }

            RulerMarker = lines[0];
            StartMarker = lines[1];
            EndMarker = lines[2];

            return true;
        }
    }
}