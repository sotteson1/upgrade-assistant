﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Windows
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class WinUIContentDialogAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "WinUIContentDialog";
        private const string Category = "Fix";

        private static readonly LocalizableString Title = "ContentDialog API needs to set XamlRoot";
        private static readonly LocalizableString MessageFormat = "Variable '{0}' should be marked const";
        private static readonly LocalizableString Description = "Detect content dialog api";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        private static readonly DiagnosticDescriptor Rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override void Initialize(AnalysisContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var node = (InvocationExpressionSyntax)context.Node;

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (!child.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                {
                    continue;
                }

                var memberAccessExpression = (MemberAccessExpressionSyntax)child!.AsNode()!;
                var memberName = memberAccessExpression.TryGetInferredMemberName();

                if (memberName != "ShowAsync")
                {
                    continue;
                }

                if (memberAccessExpression.Expression == null || memberAccessExpression.Expression.GetText().ToString().Contains("SetContentDialogRoot"))
                {
                    continue;
                }

                var type = context.SemanticModel.GetTypeInfo(memberAccessExpression.Expression).Type;

                if (type != null && type.Name == "ContentDialog")
                {
                    var diagnostic = Diagnostic.Create(Rule, memberAccessExpression.GetLocation(), node.GetText().ToString());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
