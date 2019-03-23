// ReSharper disable AssignNullToNotNullAttribute
// ReSharper disable PossibleNullReferenceException
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

using System.Collections.Generic;
using System.Linq;

using AutoProperties.Fody;

using Fody;

using FodyTools;

using JetBrains.Annotations;

using Mono.Cecil;
using Mono.Cecil.Cil;

public class ModuleWeaver : AbstractModuleWeaver
{
    public override void Execute()
    {
        // System.Diagnostics.Debugger.Launch();

        var systemReferences = new SystemReferences(this);

        new PropertyAccessorWeaver(this, systemReferences).Execute();
        new BackingFieldAccessWeaver(ModuleDefinition, this).Execute();

        CleanReferences();
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        return new[] { "mscorlib", "System", "System.Reflection", "System.Runtime", "netstandard" };
    }

    public override bool ShouldCleanReference => true;

    private void CleanReferences()
    {
        new ReferenceCleaner(ModuleDefinition, this).RemoveAttributes();
    }
}