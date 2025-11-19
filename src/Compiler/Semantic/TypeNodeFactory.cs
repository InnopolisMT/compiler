using Compiler.AST;

namespace Compiler.Semantic;

public static class TypeNodeFactory
{
    public static TypeNode CreateFromType(Type semanticType, ExpressionNode? contextExpr = null, SymbolTable? symbolTable = null)
    {
        return semanticType switch
        {
            PrimitiveType prim => new PrimitiveTypeNode { TypeName = prim.Name },
            ArrayType arr => CreateArrayTypeNode(arr, contextExpr, symbolTable),
            RecordType rec => CreateRecordTypeNode(rec, symbolTable),
            _ => CreateUserTypeNode(semanticType, symbolTable)
        };
    }

    private static TypeNode CreateArrayTypeNode(ArrayType arrType, ExpressionNode? contextExpr, SymbolTable? symbolTable)
    {
        var sizeExpr = contextExpr is ArrayInitializerNode arrInit && arrInit.Elements.Count > 0
            ? new IntegerLiteralNode { Value = arrInit.Elements.Count }
            : new IntegerLiteralNode { Value = arrType.Size };
        return new ArrayTypeNode
        {
            Size = sizeExpr,
            ElementType = CreateFromType(arrType.ElementType, null, symbolTable)
        };
    }

    private static TypeNode CreateRecordTypeNode(RecordType recType, SymbolTable? symbolTable)
    {
        var fields = recType.Fields.Select(kvp => new FieldDeclarationNode
        {
            Name = kvp.Key,
            Type = CreateFromType(kvp.Value, null, symbolTable)
        }).ToList();
        return new RecordTypeNode { Fields = fields };
    }

    private static TypeNode CreateUserTypeNode(Type semanticType, SymbolTable? symbolTable)
    {
        var typeName = semanticType.Name;
        if (symbolTable != null)
        {
            var symbol = symbolTable.Lookup(typeName);
            if (symbol != null && symbol.Kind == SymbolKind.Type && symbol.DeclarationNode is TypeDeclarationNode typeDecl)
                return new UserTypeNode { TypeName = typeDecl.Name };
        }
        return new PrimitiveTypeNode { TypeName = typeName };
    }
}

