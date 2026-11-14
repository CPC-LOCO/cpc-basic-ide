// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>Réglages de compilation choisis dans l'éditeur : machine cible, variables sans signe (lot F, étape F1a, D066).</summary>
public class OptionsTests
{
    [Fact]
    public void Par_defaut_tous_les_CPC_et_des_variables_entieres()
    {
        CompilerOptions options = CompilerOptions.Default;

        Assert.Equal(TargetMachine.AllCpc, options.Target);
        Assert.False(options.RealByDefault);
    }

    [Theory]
    [InlineData(TargetMachine.AllCpc, "Tous les CPC")]
    [InlineData(TargetMachine.Cpc464, "CPC 464")]
    [InlineData(TargetMachine.Cpc664And6128, "CPC 664/6128")]
    public void Le_nom_de_chaque_machine(TargetMachine target, string name)
    {
        Assert.Equal(name, CompilerOptions.MachineName(target));
    }

    [Fact]
    public void La_compilation_accepte_les_reglages()
    {
        CompileResult result = Compiler.CompileToDisk("10 PRINT 1", "TEST", new CompilerOptions(TargetMachine.Cpc464, true));

        Assert.True(result.Success);
    }
}
