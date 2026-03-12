using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RequireSealedImplementation
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RequireSealedImplementationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SEA001";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeClass(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
            if (classSymbol == null) return;
            if (classSymbol.IsSealed) return;

            var parentClass = classSymbol.BaseType;
            var parentClassAttributes = parentClass.GetAttributes();

            // Vérifier si la classe parent est une classe marquée [RequireSealedImplementation]
            foreach (var attr in parentClassAttributes)
            {
                if(attr.AttributeClass?.Name == "RequireSealedImplementationAttribute")
                {
                    var diagnostic = Diagnostic.Create(Rule, classDeclaration.Identifier.GetLocation(),
                        classSymbol.Name, parentClass.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }


            var allInterfaces = GetAllInterfaces(classSymbol);

            // Vérifier si la classe implémente une interface marquée [RequireSealedImplementation]
            foreach (var iface in allInterfaces)
            {
                var attrs = iface.GetAttributes();
                foreach (var attr in attrs)
                {
                    if (attr.AttributeClass?.Name == "RequireSealedImplementationAttribute")
                    {
                        var diagnostic = Diagnostic.Create(Rule, classDeclaration.Identifier.GetLocation(),
                            classSymbol.Name, iface.Name);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        // Fonction utilitaire pour récupérer toutes les interfaces, directes et héritées
        private static ImmutableArray<INamedTypeSymbol> GetAllInterfaces(INamedTypeSymbol typeSymbol)
        {
            var all = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
            foreach (var iface in typeSymbol.AllInterfaces) // AllInterfaces inclut déjà les interfaces indirectes
            {
                all.Add(iface);
            }
            return all.ToImmutable();
        }
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public sealed class RequireSealedImplementationAttribute : Attribute { }
}
