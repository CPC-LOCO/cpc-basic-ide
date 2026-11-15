// Copyright (C) 2026 RetroPoke
// Ce programme est un logiciel libre : vous pouvez le redistribuer et/ou le modifier
// selon les termes de la licence publique générale GNU, version 3.
// Il est distribué sans aucune garantie. Voir le fichier LICENSE.

using CpcBasic.Core;
using CpcBasic.Core.Codegen;
using Xunit;

namespace CpcBasic.Tests;

/// <summary>
/// Lot F1b, fondations (D068) : le format des nombres à virgule sur 5 octets, et la table de sauts
/// vers les routines de calcul de la ROM, pour le 464 comme pour le 664/6128.
/// </summary>
public class RealTests
{
    [Theory]
    [InlineData(1.0, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x81 })]
    [InlineData(0.5, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x80 })]
    [InlineData(-1.0, new byte[] { 0x00, 0x00, 0x00, 0x80, 0x81 })]
    [InlineData(10.0, new byte[] { 0x00, 0x00, 0x00, 0x20, 0x84 })]
    [InlineData(0.0, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00 })]
    [InlineData(3.0, new byte[] { 0x00, 0x00, 0x00, 0x40, 0x82 })]
    public void Les_nombres_sont_ranges_comme_le_Locomotive(double value, byte[] expected)
    {
        Assert.Equal(expected, LocoReal.Encode(value));
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(-2.5)]
    [InlineData(1e20)]
    [InlineData(-2.5e-5)]
    [InlineData(32767.0)]
    public void Un_nombre_relu_garde_sa_valeur(double value)
    {
        double back = LocoReal.Decode(LocoReal.Encode(value)!);
        Assert.Equal(value, back, Math.Abs(value) * 1e-9);
    }

    [Fact]
    public void Un_tiers_garde_9_chiffres()
    {
        Assert.Equal(1.0 / 3, LocoReal.Decode(LocoReal.Encode(1.0 / 3)!), 9);
    }

    [Fact]
    public void Trop_grand_donne_Overflow_et_trop_petit_donne_zero()
    {
        Assert.Null(LocoReal.Encode(1e39));
        Assert.True(LocoReal.MaxValue > 1.7e38 && LocoReal.MaxValue < 1.71e38);
        Assert.Equal(new byte[5], LocoReal.Encode(1e-40));
    }

    /// <summary>Le module de calcul seul, posé en &amp;4000, sur une machine de la version donnée.</summary>
    static (MiniZ80 Cpu, Z80Emitter Emitter) Machine(string module, byte version)
    {
        var emitter = new Z80Emitter(0x4000);
        RuntimeLibrary.Get(module).Emit(emitter);
        var cpu = new MiniZ80 { FirmwareVersion = version, AngleDegrees = true };
        cpu.Load(emitter.ToBytes(), 0x4000);
        cpu.Call(emitter.AddressOf(RuntimeLibrary.MathsInitLabel));
        return (cpu, emitter);
    }

    static double Add(MiniZ80 cpu, Z80Emitter emitter, double a, double b)
    {
        cpu.WriteReal(0x9000, a);
        cpu.WriteReal(0x9010, b);
        cpu.HL = 0x9000;
        cpu.DE = 0x9010;
        cpu.Call(emitter.AddressOf(FirmwareMaths.Add.Label));
        Assert.True(cpu.CarryFlag);
        return cpu.ReadReal(0x9000);
    }

    [Theory]
    [InlineData(RuntimeLibrary.MathsAllName, 0)]    // tous les CPC, lancé sur un 464 : la table est recopiée
    [InlineData(RuntimeLibrary.MathsAllName, 1)]    // tous les CPC, lancé sur un 664
    [InlineData(RuntimeLibrary.MathsAllName, 2)]    // tous les CPC, lancé sur un 6128
    [InlineData(RuntimeLibrary.Maths464Name, 0)]
    [InlineData(RuntimeLibrary.Maths6128Name, 2)]
    public void La_table_de_sauts_trouve_les_routines_de_la_machine(string module, byte version)
    {
        var (cpu, emitter) = Machine(module, version);
        Assert.False(cpu.AngleDegrees);                 // le départ met les angles en radians
        Assert.Equal(3.75, Add(cpu, emitter, 1.25, 2.5), 9);
    }

    [Fact]
    public void Sur_un_464_la_division_n_est_pas_prise_pour_un_sinus()
    {
        // &BD88 est DIVISION sur le 6128 mais SIN sur le 464 : la table doit éviter la confusion.
        var (cpu, emitter) = Machine(RuntimeLibrary.MathsAllName, 0);
        cpu.WriteReal(0x9000, 1);
        cpu.WriteReal(0x9010, 4);
        cpu.HL = 0x9000;
        cpu.DE = 0x9010;
        cpu.Call(emitter.AddressOf(FirmwareMaths.Divide.Label));
        Assert.Equal(0.25, cpu.ReadReal(0x9000), 9);
    }

    [Fact]
    public void Entier_vers_virgule_et_retour()
    {
        var (cpu, emitter) = Machine(RuntimeLibrary.MathsAllName, 2);
        cpu.HL = 1234;
        cpu.A = 0x80;                                   // négatif
        cpu.DE = 0x9000;
        cpu.Call(emitter.AddressOf(FirmwareMaths.IntegerToReal.Label));
        Assert.Equal(-1234.0, cpu.ReadReal(0x9000));

        cpu.WriteReal(0x9000, 2.5);
        cpu.HL = 0x9000;
        cpu.Call(emitter.AddressOf(FirmwareMaths.RealToInteger.Label));
        Assert.True(cpu.CarryFlag);
        Assert.Equal(3, (int)cpu.HL);                      // arrondi : .5 vers le haut

        cpu.WriteReal(0x9000, 70000);
        cpu.HL = 0x9000;
        cpu.Call(emitter.AddressOf(FirmwareMaths.RealToInteger.Label));
        Assert.False(cpu.CarryFlag);                    // trop grand pour un entier
    }

    [Theory]
    [InlineData(TargetMachine.AllCpc, RuntimeLibrary.MathsAllName)]
    [InlineData(TargetMachine.Cpc464, RuntimeLibrary.Maths464Name)]
    [InlineData(TargetMachine.Cpc664And6128, RuntimeLibrary.Maths6128Name)]
    public void Le_module_suit_le_reglage_Machine(TargetMachine target, string module)
    {
        Assert.Equal(module, RuntimeLibrary.MathsModuleFor(target));
    }
}
