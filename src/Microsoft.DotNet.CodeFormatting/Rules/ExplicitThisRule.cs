﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.DotNet.CodeFormatting.Rules
{
    [LocalSemanticRule(ExplicitThisRule.Name, ExplicitThisRule.Description, LocalSemanticRuleOrder.RemoveExplicitThisRule)]
    internal sealed class ExplicitThisRule : CSharpOnlyFormattingRule, ILocalSemanticFormattingRule
    {
        internal const string Name = "ExplicitThis";
        internal const string Description = "Remove explicit this/Me prefixes on expressions except where necessary";

        private sealed class ExplicitThisRewriter : CSharpSyntaxRewriter
        {
            private readonly Document _document;
            private readonly CancellationToken _cancellationToken;
            private SemanticModel _semanticModel;
            private bool _addedAnnotations;

            internal bool AddedAnnotations
            {
                get { return _addedAnnotations; }
            }

            internal ExplicitThisRewriter(Document document, CancellationToken cancellationToken)
            {
                _document = document;
                _cancellationToken = cancellationToken;
            }

            public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                node = (MemberAccessExpressionSyntax)base.VisitMemberAccessExpression(node);
                string name = node.Name.Identifier.ValueText;
                if (node.Expression != null &&
                    node.Expression.Kind() == SyntaxKind.ThisExpression &&
                    IsPrivateField(node))
                {
                    _addedAnnotations = true;
                    return node.WithAdditionalAnnotations(Simplifier.Annotation);
                }

                return node;
            }

            private bool IsPrivateField(MemberAccessExpressionSyntax memberSyntax)
            {
                if (_semanticModel == null)
                {
                    _semanticModel = _document.GetSemanticModelAsync(_cancellationToken).Result;
                }

                SymbolInfo symbolInfo = _semanticModel.GetSymbolInfo(memberSyntax, _cancellationToken);
                if (symbolInfo.Symbol != null && symbolInfo.Symbol.Kind == SymbolKind.Field)
                {
                    var field = (IFieldSymbol)symbolInfo.Symbol;
                    return field.DeclaredAccessibility == Accessibility.Private;
                }

                return false;
            }
        }

        public async Task<SyntaxNode> ProcessAsync(Document document, SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            var rewriter = new ExplicitThisRewriter(document, cancellationToken);
            SyntaxNode newNode = rewriter.Visit(syntaxNode);
            if (!rewriter.AddedAnnotations)
            {
                return syntaxNode;
            }

            document = await Simplifier.ReduceAsync(document.WithSyntaxRoot(newNode), cancellationToken: cancellationToken);
            return await document.GetSyntaxRootAsync(cancellationToken);
        }
    }
}