using System.Collections.Generic;
using NUnit.Framework;
using SDKHub.Editor;

namespace SDKHub.Tests
{
    public class ModuleRegistryTests
    {
        [Test]
        public void EveryModuleHasAUniqueId()
        {
            var seen = new HashSet<string>();
            foreach (var module in SdkRegistry.Modules) Assert.IsTrue(seen.Add(module.Id), "Duplicate id: " + module.Id);
        }

        [Test]
        public void EveryModuleHasItsOwnDefineSymbol()
        {
            var seen = new HashSet<string>();
            foreach (var module in SdkRegistry.Modules)
            {
                Assert.IsTrue(module.DefineSymbol.StartsWith("SDKHUB_"), module.Id + " define symbol");
                Assert.IsTrue(seen.Add(module.DefineSymbol), "Duplicate define symbol: " + module.DefineSymbol);
            }
        }

        [Test]
        public void PackageModulesNameAPackage()
        {
            foreach (var module in SdkRegistry.Modules)
            {
                if (module.InstallType == InstallType.Registry || module.InstallType == InstallType.GitUrl)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(module.PackageName), module.Id + " needs a package name");
                    Assert.IsFalse(string.IsNullOrEmpty(module.InstallIdentifier), module.Id + " needs an install identifier");
                }
            }
        }

        [Test]
        public void ExternalModulesCanBeDetected()
        {
            foreach (var module in SdkRegistry.Modules)
            {
                if (module.InstallType == InstallType.External) Assert.IsFalse(string.IsNullOrEmpty(module.DetectionType), module.Id);
            }
        }

        [Test]
        public void PrerequisitesPointToRealModules()
        {
            foreach (var module in SdkRegistry.Modules)
            {
                foreach (var id in module.Prerequisites) Assert.IsNotNull(SdkRegistry.Find(id), module.Id + " needs " + id);
            }
        }

        [Test]
        public void ScopedRegistriesIncludeTheirOwnPackage()
        {
            foreach (var module in SdkRegistry.Modules)
            {
                if (module.ScopedRegistry == null) continue;
                CollectionAssert.Contains(module.ScopedRegistry.Scopes, module.PackageName, module.Id);
            }
        }

        [Test]
        public void GlyphsFitInTheTile()
        {
            foreach (var module in SdkRegistry.Modules)
            {
                Assert.IsFalse(string.IsNullOrEmpty(module.Glyph), module.Id);
                Assert.LessOrEqual(module.Glyph.Length, 2, module.Id);
            }
        }

        [Test]
        public void EveryModuleCalculatesAStatusWithoutErrors()
        {
            foreach (var module in SdkRegistry.Modules)
            {
                Assert.DoesNotThrow(() => module.GetStatus(), module.Id);
                Assert.IsNotNull(module.Checks, module.Id);
            }
        }
    }
}
