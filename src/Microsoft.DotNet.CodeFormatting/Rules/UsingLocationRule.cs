// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.DotNet.CodeFormatting.Rules
{
    /// <summary>
    /// This will ensure that using directives are placed outside of the namespace.
    /// </summary>
    [SyntaxRule(UsingLocationRule.Name, UsingLocationRule.Description, SyntaxRuleOrder.UsingLocationFormattingRule, DefaultRule = false)]
    internal sealed class UsingLocationRule : CSharpOnlyFormattingRule, ISyntaxFormattingRule
    {
        internal const string Name = "UsingLocation";
        internal const string Description = "Place using directives outside namespace declarations";

        public SyntaxNode Process(SyntaxNode syntaxNode, string languageName)
        {
            var root = syntaxNode as CompilationUnitSyntax;
            if (root == null)
                return syntaxNode;
            System.Collections.Generic.List<NamespaceDeclarationSyntax> namespaceDeclarationList = root.Members.OfType<NamespaceDeclarationSyntax>().ToList();
            if (namespaceDeclarationList.Count != 1)
            {
                return syntaxNode;
            }

            NamespaceDeclarationSyntax namespaceDeclaration = namespaceDeclarationList.Single();
            SyntaxList<UsingDirectiveSyntax> usingList = namespaceDeclaration.Usings;
            if (usingList.Count == 0)
            {
                return syntaxNode;
            }

            // Moving a using with an alias out of a namespace is an operation which requires
            // semantic knowledge to get correct.
            if (usingList.Any(x => x.Alias != null))
            {
                return syntaxNode;
            }

            // We don't have the capability to safely move usings which are embedded inside an #if
            // directive.  
            //
            //  #if COND
            //  using NS1;
            //  #endif
            //
            // At the time there isn't a great way (that we know of) for detecting this particular 
            // case.  Instead we simply don't do this rewrite if the file contains any #if directives.
            if (root.DescendantTrivia().Any(x => x.Kind() == SyntaxKind.IfDirectiveTrivia))
            {
                return syntaxNode;
            }

            CompilationUnitSyntax newRoot = root;
            newRoot = newRoot.ReplaceNode(namespaceDeclaration, namespaceDeclaration.WithUsings(SyntaxFactory.List<UsingDirectiveSyntax>()));
            newRoot = newRoot.WithUsings(newRoot.Usings.AddRange(namespaceDeclaration.Usings));

            return newRoot;
        }
    }
}
