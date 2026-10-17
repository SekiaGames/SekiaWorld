using System.Collections.Generic;
using System.Linq;
using System.Text;
using ET.Analyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ET.Generator;

/// <summary>
/// 为直接实现 IPool 的 partial 类自动生成 void IPool.Clear()，回收进对象池前清理残留状态。
/// 规则（移植自 ET10）：
/// 1. 只处理非抽象、非嵌套、直接声明 IPool、且是 partial 的类；
/// 2. 类里若已手写 IPool.Clear（显式实现）则跳过；
/// 3. 成员里出现需要手工清理的类型（IDisposable/集合等且无无参 Clear）则整体跳过，避免生成错误的清理逻辑；
/// 4. 字段/属性：可自动 Clear 的集合调 ?.Clear()，其余可写成员置 default，基础设施成员（IsFromPool 等）不动；
/// 5. 基类是集合类型（自带无参 Clear）时在生成的 Clear 里先调 base.Clear()。
/// </summary>
[Generator(LanguageNames.CSharp)]
public class IPoolClearGenerator: ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxContextReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not SyntaxContextReceiver receiver || receiver.ClassDeclarations.Count == 0)
        {
            return;
        }

        foreach (ClassDeclarationSyntax classDeclarationSyntax in receiver.ClassDeclarations)
        {
            SemanticModel semanticModel = context.Compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol typeSymbol)
            {
                continue;
            }

            if (!ShouldGenerate(typeSymbol, classDeclarationSyntax))
            {
                continue;
            }

            string? namespaceName = typeSymbol.GetNameSpace();
            if (namespaceName == null)
            {
                continue;
            }

            StringBuilder assignmentBuilder = new();
            if (NeedsBaseClear(typeSymbol))
            {
                assignmentBuilder.AppendLine("            base.Clear();");
            }

            List<string> assignments = GetAssignments(typeSymbol);
            foreach (string assignment in assignments)
            {
                assignmentBuilder.AppendLine($"            {assignment}");
            }

            string typeParameterList = GetTypeParameterList(typeSymbol);
            string constraintClauses = GetTypeParameterConstraintClauses(typeSymbol);
            string code = $$"""
namespace {{namespaceName}}
{
    public partial class {{typeSymbol.Name}}{{typeParameterList}}{{constraintClauses}}
    {
        void global::ET.IPool.Clear()
        {
{{assignmentBuilder}}
        }
    }
}
""";

            string hintName = SanitizeHintName(typeSymbol.ToDisplayString());
            context.AddSource($"IPoolClearGenerator.{hintName}.g.cs", code);
        }
    }

    private static bool ShouldGenerate(INamedTypeSymbol typeSymbol, ClassDeclarationSyntax classDeclarationSyntax)
    {
        if (typeSymbol.IsAbstract || typeSymbol.ContainingType != null)
        {
            return false;
        }

        if (!ImplementsIPoolDirectly(typeSymbol))
        {
            return false;
        }

        if (!classDeclarationSyntax.IsPartial())
        {
            return false;
        }

        if (HasExplicitIPoolClear(typeSymbol))
        {
            return false;
        }

        return !NeedsManualClear(typeSymbol);
    }

    internal static bool HasExplicitIPoolClear(INamedTypeSymbol typeSymbol)
    {
        INamedTypeSymbol? current = typeSymbol;
        while (current != null)
        {
            if (current.GetMembers().OfType<IMethodSymbol>().Any(IsExplicitIPoolClear))
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    private static bool IsExplicitIPoolClear(IMethodSymbol methodSymbol)
    {
        if (methodSymbol.Parameters.Length != 0)
        {
            return false;
        }

        return methodSymbol.ExplicitInterfaceImplementations.Any(interfaceMethodSymbol =>
            interfaceMethodSymbol.Name == Definition.ClearMethod &&
            interfaceMethodSymbol.ContainingType.ToString() == Definition.IPoolInterface);
    }

    internal static bool ImplementsIPoolDirectly(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.Interfaces.Any(interfaceSymbol => interfaceSymbol.ToString() == Definition.IPoolInterface);
    }

    internal static bool NeedsManualClear(INamedTypeSymbol typeSymbol)
    {
        foreach (ISymbol member in typeSymbol.GetMembers())
        {
            ITypeSymbol? memberType = member switch
            {
                IFieldSymbol { IsStatic: false, IsConst: false, IsReadOnly: false, IsImplicitlyDeclared: false } fieldSymbol => fieldSymbol.Type,
                IPropertySymbol { IsStatic: false, IsIndexer: false, SetMethod: not null } propertySymbol => propertySymbol.Type,
                _ => null
            };

            if (memberType == null || IsInfrastructureMember(member.Name))
            {
                continue;
            }

            if (IsManualClearType(memberType))
            {
                return true;
            }
        }

        return false;
    }

    private static List<string> GetAssignments(INamedTypeSymbol typeSymbol)
    {
        List<string> assignments = new();
        foreach (ISymbol member in typeSymbol.GetMembers())
        {
            switch (member)
            {
                case IFieldSymbol { IsStatic: false, IsConst: false, IsReadOnly: false, IsImplicitlyDeclared: false } fieldSymbol:
                {
                    if (IsInfrastructureMember(fieldSymbol.Name) || fieldSymbol.Name.Contains("<"))
                    {
                        continue;
                    }

                    if (CanAutoClearMember(fieldSymbol.Type))
                    {
                        assignments.Add($"this.{fieldSymbol.Name}?.Clear();");
                        break;
                    }

                    if (IsManualClearType(fieldSymbol.Type))
                    {
                        continue;
                    }

                    assignments.Add($"this.{fieldSymbol.Name} = default;");
                    break;
                }
                case IPropertySymbol { IsStatic: false, IsIndexer: false, SetMethod: not null } propertySymbol:
                {
                    if (IsInfrastructureMember(propertySymbol.Name))
                    {
                        continue;
                    }

                    if (CanAutoClearMember(propertySymbol.Type))
                    {
                        assignments.Add($"this.{propertySymbol.Name}?.Clear();");
                        break;
                    }

                    if (IsManualClearType(propertySymbol.Type))
                    {
                        continue;
                    }

                    assignments.Add($"this.{propertySymbol.Name} = default;");
                    break;
                }
            }
        }

        return assignments;
    }

    private static bool NeedsBaseClear(INamedTypeSymbol typeSymbol)
    {
        INamedTypeSymbol? baseType = typeSymbol.BaseType;
        while (baseType != null && baseType.SpecialType != SpecialType.System_Object)
        {
            if (IsCollectionLikeType(baseType) && HasAccessibleParameterlessClear(baseType))
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    private static bool HasAccessibleParameterlessClear(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.GetMembers(Definition.ClearMethod).OfType<IMethodSymbol>().Any(methodSymbol =>
            !methodSymbol.IsStatic &&
            methodSymbol.Parameters.Length == 0 &&
            methodSymbol.DeclaredAccessibility is Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal);
    }

    private static bool IsCollectionLikeType(ITypeSymbol typeSymbol)
    {
        return Implements(typeSymbol, "System.Collections.ICollection") ||
               Implements(typeSymbol, "System.Collections.IDictionary") ||
               ImplementsOriginalDefinition(typeSymbol, "System.Collections.Generic.ICollection<T>") ||
               ImplementsOriginalDefinition(typeSymbol, "System.Collections.Generic.ISet<T>") ||
               ImplementsOriginalDefinition(typeSymbol, "System.Collections.Generic.IDictionary<TKey, TValue>");
    }

    private static bool IsManualClearType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol.IsValueType || typeSymbol.SpecialType == SpecialType.System_String || typeSymbol.SpecialType == SpecialType.System_Object)
        {
            return false;
        }

        if (CanAutoClearMember(typeSymbol))
        {
            return false;
        }

        return Implements(typeSymbol, "System.IDisposable") ||
               Implements(typeSymbol, "System.Collections.IEnumerable") ||
               ImplementsOriginalDefinition(typeSymbol, "System.Collections.Generic.IEnumerable<T>") ||
               Implements(typeSymbol, "System.Collections.IDictionary") ||
               ImplementsOriginalDefinition(typeSymbol, "System.Collections.Generic.IDictionary<TKey, TValue>");
    }

    private static bool CanAutoClearMember(ITypeSymbol typeSymbol)
    {
        return !typeSymbol.IsValueType &&
               typeSymbol is INamedTypeSymbol namedTypeSymbol &&
               IsCollectionLikeType(namedTypeSymbol) &&
               HasAccessibleParameterlessClear(namedTypeSymbol);
    }

    private static bool Implements(ITypeSymbol typeSymbol, string interfaceName)
    {
        return typeSymbol.AllInterfaces.Any(interfaceSymbol => interfaceSymbol.ToString() == interfaceName);
    }

    private static bool ImplementsOriginalDefinition(ITypeSymbol typeSymbol, string interfaceName)
    {
        return typeSymbol.AllInterfaces.Any(interfaceSymbol => interfaceSymbol.OriginalDefinition.ToString() == interfaceName);
    }

    private static bool IsInfrastructureMember(string memberName)
    {
        return memberName is "IsFromPool" or "InstanceId" or "IScene" or "ViewGO";
    }

    private static string SanitizeHintName(string hintName)
    {
        return hintName.Replace(".", "_")
                .Replace("<", "_")
                .Replace(">", "_")
                .Replace(",", "_")
                .Replace(" ", "");
    }

    private static string GetTypeParameterList(INamedTypeSymbol typeSymbol)
    {
        if (!typeSymbol.IsGenericType)
        {
            return string.Empty;
        }

        return $"<{string.Join(", ", typeSymbol.TypeParameters.Select(typeParameter => typeParameter.Name))}>";
    }

    private static string GetTypeParameterConstraintClauses(INamedTypeSymbol typeSymbol)
    {
        if (!typeSymbol.IsGenericType)
        {
            return string.Empty;
        }

        List<string> clauses = new();
        foreach (ITypeParameterSymbol typeParameter in typeSymbol.TypeParameters)
        {
            List<string> constraints = new();
            if (typeParameter.HasUnmanagedTypeConstraint)
            {
                constraints.Add("unmanaged");
            }
            else if (typeParameter.HasValueTypeConstraint)
            {
                constraints.Add("struct");
            }
            else if (typeParameter.HasReferenceTypeConstraint)
            {
                constraints.Add("class");
            }
            else if (typeParameter.HasNotNullConstraint)
            {
                constraints.Add("notnull");
            }

            constraints.AddRange(typeParameter.ConstraintTypes.Select(type =>
                    type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

            if (typeParameter.HasConstructorConstraint && !typeParameter.HasValueTypeConstraint && !typeParameter.HasUnmanagedTypeConstraint)
            {
                constraints.Add("new()");
            }

            if (constraints.Count > 0)
            {
                clauses.Add($" where {typeParameter.Name} : {string.Join(", ", constraints)}");
            }
        }

        return string.Concat(clauses);
    }

    private class SyntaxContextReceiver: ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> ClassDeclarations { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax)
            {
                this.ClassDeclarations.Add(classDeclarationSyntax);
            }
        }
    }
}
