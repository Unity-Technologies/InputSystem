using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unity.InputSystem.SourceGenerators;

public class InputActionsSyntaxReceiver : ISyntaxReceiver
{
	public List<MemberAccessExpressionSyntax> InputActionsReferences { get; }

	public InputActionsSyntaxReceiver()
	{
		InputActionsReferences = new List<MemberAccessExpressionSyntax>();
	}

	public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
	{
		if (syntaxNode is MemberAccessExpressionSyntax memberAccessSyntaxNode &&
		    memberAccessSyntaxNode.Expression is IdentifierNameSyntax identifierNameSyntax)
		{
			if (identifierNameSyntax.Identifier.ValueText == "InputActions")
				InputActionsReferences.Add(memberAccessSyntaxNode);
		}
	}
}