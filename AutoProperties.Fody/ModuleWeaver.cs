namespace AutoProperties.Fody
{
    using System.Collections.Generic;

    using FodyTools;

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
}