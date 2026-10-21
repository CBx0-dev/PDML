using PDML.Compiler.Binding.Nodes;
using PDML.Compiler.Binding.Nodes.Members;
using PDML.Compiler.Binding.Types;
using PDML.Compiler.Common;
using PDML.Compiler.Diagnostics;
using PDML.Compiler.Parsing.Nodes;
using PDML.Compiler.Parsing.Nodes.Members;

namespace PDML.Compiler.Binding;

public sealed class Binder
{
    private readonly DiagnosticBag _diagnostics;
    private readonly Module _module;
    private readonly Dictionary<BoundTypeDefinition, Dictionary<string, BoundTypeDefinitionProperty>> _types;
    private readonly Dictionary<string, Location> _seenNames;

    private Binder(DiagnosticBag diagnostics, Module module)
    {
        _diagnostics = diagnostics;
        _module = module;
        _types = new Dictionary<BoundTypeDefinition, Dictionary<string, BoundTypeDefinitionProperty>>();
        _seenNames = new Dictionary<string, Location>();
    }

    private BoundModule BindModule()
    {
        BindTypeDefinitionHeaders();
        BindTypeDefinitionInheritances();
        BindTypeDefinitionBasicBodies();
        BindTypeCompositionBodies();

        return new BoundModule(_module.Location, _types.Keys.ToArray());
    }

    private void BindTypeDefinitionHeaders()
    {
        if (_diagnostics.HasErrors)
        {
            return;
        }

        foreach (TypeDefinitionMember definition in _module.TypeDefinitions)
        {
            BindTypeDefinitionHeader(definition);
        }
    }

    private void BindTypeDefinitionHeader(TypeDefinitionMember definition)
    {
        string name = definition.Name.Value;
        if (_seenNames.TryGetValue(name, out Location? previousDeclaration))
        {
            _diagnostics.Report(BindingError.DuplicateName, definition.Name.Location, [previousDeclaration], name);
            return;
        }

        _seenNames.Add(name, definition.Name.Location);

        Dictionary<string, BoundTypeDefinitionProperty> body = new Dictionary<string, BoundTypeDefinitionProperty>();
        BoundTypeDefinition type = new BoundTypeDefinition(definition.Name.Location, name, null, body);

        _types.Add(type, body);
    }

    private void BindTypeDefinitionInheritances()
    {
        if (_diagnostics.HasErrors)
        {
            return;
        }

        foreach (TypeDefinitionMember definition in _module.TypeDefinitions)
        {
            if (definition.InheritingType is null)
            {
                continue;
            }

            BoundTypeDefinition type = _types.Keys.First(type => type.Name == definition.Name.Value);
            BindTypeDefinitionInheritance(definition, type);
        }
    }

    private void BindTypeDefinitionInheritance(TypeDefinitionMember definition, BoundTypeDefinition type)
    {
        // Is only called to satisfy the type system
        if (definition.InheritingType is null)
        {
            return;
        }

        if (definition.InheritingType.Generics is not null)
        {
            _diagnostics.Report(BindingError.FunctionNotAllowed, definition.InheritingType.Name.Location, definition.InheritingType.Name.Value);
            return;
        }

        TypeRef? typeRef = FindType(definition.InheritingType, false);
        if (typeRef is null)
        {
            return;
        }

        if (typeRef is not TypeDefinitionTypeRef typeDefinitionTypeRef)
        {
            _diagnostics.Report(BindingError.IntrinsicTypeNotAllowed, definition.InheritingType.Name.Location, definition.InheritingType.Name.Value);
            return;
        }

        BoundTypeDefinition? parentType = typeDefinitionTypeRef.Definition;
        List<Location> locations = new List<Location>();

        while (parentType is not null)
        {
            if (parentType.Location is not null)
            {
                locations.Add(parentType.Location);
            }

            if (parentType == type)
            {
                _diagnostics.Report(BindingError.CircularInheritance, definition.Name.Location, locations.ToArray(), definition.Name.Value);
                return;
            }

            parentType = parentType.InheritingType?.Definition;
        }

        type.InheritingType = typeDefinitionTypeRef;
    }

    private void BindTypeDefinitionBasicBodies()
    {
        if (_diagnostics.HasErrors)
        {
            return;
        }

        foreach (TypeDefinitionMember definition in _module.TypeDefinitions)
        {
            BoundTypeDefinition type = _types.Keys.First(type => type.Name == definition.Name.Value);
            Dictionary<string, BoundTypeDefinitionProperty> body = _types[type];

            BindTypeDefinitionBasicBody(definition, type, body);
        }
    }

    private void BindTypeDefinitionBasicBody(TypeDefinitionMember definition, BoundTypeDefinition type, Dictionary<string, BoundTypeDefinitionProperty> body)
    {
        foreach (TypeDefinitionItem item in definition.Items)
        {
            if (item is not TypeDefinitionItemBody itemBody)
            {
                continue;
            }

            foreach (TypeDefinitionBodyProperty property in itemBody.Properties)
            {

                if (property.TypeClause.TypeSignature.Generics is not null)
                {
                    _diagnostics.Report(
                        BindingError.FunctionNotAllowed,
                        property.TypeClause.TypeSignature.Name.Location,
                        property.TypeClause.TypeSignature.Name.Value
                    );
                    continue;
                }

                string propertyName = property.Name.Value;
                TypeRef? typeRef = FindType(property.TypeClause, false);
                if (typeRef is null)
                {
                    continue;
                }

                BoundTypeDefinitionProperty boundProperty = new BoundTypeDefinitionProperty(property.Name.Location, typeRef);
                if (body.TryGetValue(propertyName, out BoundTypeDefinitionProperty? existingProperty))
                {
                    List<Location> locations = new List<Location>();
                    if (existingProperty.Location is not null)
                    {
                        locations.Add(existingProperty.Location);
                    }

                    _diagnostics.Report(
                        BindingError.DuplicateProperty,
                        property.Name.Location,
                        locations.ToArray(),
                        propertyName
                    );
                }
                else
                {
                    body.Add(propertyName, boundProperty);
                }
            }
        }
    }

    private void BindTypeCompositionBodies()
    {
        if (_diagnostics.HasErrors)
        {
            return;
        }

        foreach (TypeDefinitionMember definition in _module.TypeDefinitions)
        {
            BoundTypeDefinition type = _types.Keys.First(type => type.Name == definition.Name.Value);
            Dictionary<string, BoundTypeDefinitionProperty> body = _types[type];

            BindCompositionBody(definition, type, body);
        }
    }

    private void BindCompositionBody(TypeDefinitionMember definition, BoundTypeDefinition type, Dictionary<string, BoundTypeDefinitionProperty> body)
    {
        foreach (TypeDefinitionItem item in definition.Items)
        {
            if (item is not TypeDefinitionItemTarget itemTarget)
            {
                continue;
            }

            TypeRef? typeRef = FindType(itemTarget.Type, true);
            if (typeRef is null)
            {
                continue;
            }

            if (typeRef is not TypeDefinitionTypeRef typeDefinitionTypeRef)
            {
                _diagnostics.Report(BindingError.IntrinsicTypeNotAllowed, itemTarget.Location, itemTarget.Type.Name.Value);
                continue;
            }

            foreach ((string propertyName, BoundTypeDefinitionProperty property) in typeDefinitionTypeRef.Definition.Properties)
            {
                if (body.TryGetValue(propertyName, out BoundTypeDefinitionProperty? existingProperty))
                {
                    List<Location> locations = new List<Location>();
                    if (existingProperty.Location is not null)
                    {
                        locations.Add(existingProperty.Location);
                    }

                    _diagnostics.Report(
                        BindingError.DuplicatePropertyComposition,
                        item.Location,
                        locations.ToArray(),
                        propertyName,
                        itemTarget.Type.Name.Value
                    );
                }
                else
                {
                    body.Add(propertyName, property);
                }
            }
        }
    }

    private TypeRef? FindType(TypeClause target, bool allowSpecialFunctions)
        => FindType(target.TypeSignature, allowSpecialFunctions);

    private TypeRef? FindType(TypeSignature target, bool allowSpecialFunctions)
    {
        if (target.Generics is not null)
        {
            if (!allowSpecialFunctions)
            {
                return null;
            }

            return ExecuteTypeFunction();
        }

        string name = target.Name.Value;
        switch (name)
        {
            case "uint8":
                return IntrinsicTypeRef.Uint8;
            case "uint16":
                return IntrinsicTypeRef.Uint16;
            case "uint32":
                return IntrinsicTypeRef.Uint32;
            case "uint64":
                return IntrinsicTypeRef.Uint64;
            case "int8":
                return IntrinsicTypeRef.Int8;
            case "int16":
                return IntrinsicTypeRef.Int16;
            case "int32":
                return IntrinsicTypeRef.Int32;
            case "int64":
                return IntrinsicTypeRef.Int64;
            case "char":
                return IntrinsicTypeRef.Char;
            case "bool":
                return IntrinsicTypeRef.Bool;
            case "string":
                return IntrinsicTypeRef.String;
        }

        BoundTypeDefinition? definition = _types.Keys.FirstOrDefault(t => t.Name == name);
        if (definition is not null)
        {
            return new TypeDefinitionTypeRef(definition);
        }

        _diagnostics.Report(BindingError.UnknownType, target.Name.Location, target.Name.Value);
        return null;
    }

    private TypeRef? ExecuteTypeFunction()
    {
        // TODO implement special functions like Pick or Omit
        return null;
    }

    public static BoundModule BindModule(DiagnosticBag diagnostics, Module module)
    {
        Binder binder = new Binder(diagnostics, module);

        return binder.BindModule();
    }
}