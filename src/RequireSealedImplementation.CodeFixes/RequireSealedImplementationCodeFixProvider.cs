using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RequireSealedImplementation
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RequireSealedImplementationCodeFixProvider)), Shared]
    public class RequireSealedImplementationCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(RequireSealedImplementationAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }
        
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            var diagnostic = context.Diagnostics[0];
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Trouver le noeud ClassDeclaration sur lequel appliquer le fix
            var classDecl = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                .OfType<ClassDeclarationSyntax>().FirstOrDefault();

            if (classDecl == null) return;

            string fixMessageTitle = string.Format(CodeFixResources.CodeFixTitle, classDecl.Identifier.ValueText);

            // Enregistrer le CodeFix
            context.RegisterCodeFix(
                CodeAction.Create(
                    fixMessageTitle,
                    c => MakeClassSealedAsync(context.Document, classDecl, c),
                    equivalenceKey: fixMessageTitle),
                diagnostic);
        }

        private async Task<Document> MakeClassSealedAsync(Document document, ClassDeclarationSyntax classDecl, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // Ajouter le mot-clé 'sealed' devant le class
            var newClassDecl = classDecl;

            // Vérifie si 'sealed' n'existe pas déjà
            if (!newClassDecl.Modifiers.Any(SyntaxKind.SealedKeyword))
            {
                var modifiers = newClassDecl.Modifiers;
                modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.SealedKeyword));
                newClassDecl = newClassDecl.WithModifiers(modifiers);
            }

            editor.ReplaceNode(classDecl, newClassDecl);
            return editor.GetChangedDocument();
        }
    }
}
