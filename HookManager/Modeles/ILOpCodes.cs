using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using HookManager.Helpers;

namespace HookManager.Modeles
{
    internal static class ILOpCodes
    {
        private static List<OpCode> _listeOpCodes;

        internal static void ChargeOpCodes()
        {
            if (_listeOpCodes != null)
                _listeOpCodes.Clear();
            _listeOpCodes = new();
            foreach (FieldInfo fi in typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public).Where(fi => fi.FieldType == typeof(OpCode)))
            {
                _listeOpCodes.Add((OpCode)fi.GetValue(null));
            }
        }

        internal static List<OpCode> ListeOpCodes
        {
            get
            {
                if (_listeOpCodes == null)
                    ChargeOpCodes();
                return _listeOpCodes;
            }
        }

        internal static OpCode RetourneOpCode(byte[] commande, ref int offset, out byte complement)
        {
            OpCode retour = OpCodes.Nop;
            byte cmd = commande[offset++];
            complement = 0;
            if (cmd == 0xFE)
                complement = commande[offset++];
            foreach (OpCode code in ListeOpCodes)
                if ((code.Value == cmd && complement == 0) || (complement > 0 && code.Size > 1 && ((code.Value & 0xFF) == cmd)))
                {
                    retour = code;
                    break;
                }
            return retour;
        }

        internal static List<ILCommande> LireMethodBody(this MethodBase methodeACopier)
        {
            byte[] listeCodes = methodeACopier.GetMethodBody().GetILAsByteArray();
            List<ILCommande> retour = new();
            List<int> listeGoto = new();
            int offset;
            ILCommande cmd;
            int numLabel = 0;
            for (offset = 0; offset <= listeCodes.Length; offset++)
            {
                if (offset > 0)
                    offset--;
                cmd = new();
                cmd.offset = offset;
                OpCode commande = RetourneOpCode(listeCodes, ref offset, out byte complement);
                cmd.CodeIL = commande;
                cmd.ComplementCodeIL = complement;
                if (listeGoto.Contains(offset))
                    cmd.MarqueDebutLabel(numLabel++);
                switch (commande.OperandType)
                {
                    case OperandType.InlineBrTarget:
                        cmd.Param = listeCodes.ReadInt32(ref offset);
                        break;
                    case OperandType.InlineField:
                        cmd.Param = methodeACopier.Module.ResolveField(listeCodes.ReadInt32(ref offset));
                        break;
                    case OperandType.InlineI:
                        cmd.Param = listeCodes.ReadInt32(ref offset);
                        break;
                    case OperandType.InlineI8:
                        cmd.Param = listeCodes.ReadInt64(ref offset);
                        break;
                    case OperandType.InlineMethod:
                        cmd.Param = methodeACopier.Module.ResolveMethod(listeCodes.ReadInt32(ref offset));
                        break;
                    case OperandType.InlineNone:
                        break;
                    case OperandType.InlineR:
                        cmd.Param = listeCodes.ReadDouble(ref offset);
                        break;
                    case OperandType.InlineSig:
                        int signature = listeCodes.ReadInt32(ref offset);
                        /*byte[] donnees = methodeACopier.Module.ResolveSignature(signature);
                        int offsetDonnees = 0;
                        ILCall monCall = new();
                        byte call = donnees[offsetDonnees++];
                        if ((call & 0x20) != 0)
                        {
                            monCall.avecThis = true;
                            monCall.call = (byte)(call & ~0x20);
                        }
                        if ((call & 0x40) != 0)
                        {
                            monCall.avecThis = true;
                            monCall.call = (byte)(call & ~0x40);
                        }
                        monCall.ConventionAppel = (ECallConvention)monCall.call;
                        if ((call & 0x10) != 0)
                            donnees.ReadCompressedUInt32(ref offsetDonnees);
                        uint nbParam = donnees.ReadCompressedUInt32(ref offsetDonnees);
                        cmd.Param = monCall;*/
                        cmd.Param = signature;
                        break;
                    case OperandType.InlineString:
                        cmd.Param = methodeACopier.Module.ResolveString(listeCodes.ReadInt32(ref offset));
                        break;
                    case OperandType.InlineSwitch:
                        int taille = listeCodes.ReadInt32(ref offset);
                        int[] tableau = new int[taille];
                        for (int i = 0; i < taille; i++)
                        {
                            tableau[i] = listeCodes.ReadInt32(ref offset);
                            if (!listeGoto.Contains(tableau[i]))
                                listeGoto.Add(tableau[i]);
                        }
                        cmd.Param = tableau;
                        break;
                    case OperandType.InlineTok:
                        cmd.Param = methodeACopier.Module.ResolveMember(listeCodes.ReadInt32(ref offset));
                        break;
                    case OperandType.InlineType:
                        cmd.Param = methodeACopier.Module.ResolveType(listeCodes.ReadInt32(ref offset));
                        break;
                    case OperandType.InlineVar:
                        cmd.Param = listeCodes.ReadInt16(ref offset);
                        break;
                    case OperandType.ShortInlineBrTarget:
                        cmd.Param = listeCodes.ReadSByte(ref offset) + offset;
                        listeGoto.Add(int.Parse(cmd.Param.ToString()));
                        break;
                    case OperandType.ShortInlineI:
                        if (commande == OpCodes.Ldc_I4_S)
                            cmd.Param = listeCodes.ReadSByte(ref offset);
                        else
                            cmd.Param = listeCodes[offset++];
                        break;
                    case OperandType.ShortInlineR:
                        cmd.Param = listeCodes.ReadSingle(ref offset);
                        break;
                    case OperandType.ShortInlineVar:
                        cmd.Param = listeCodes[offset++];
                        break;
                    default:
                        throw new NotImplementedException($"OperandType inconnu ({commande.OperandType:G})");
                }
                retour.Add(cmd);
            }

            foreach (ILCommande instruction in retour)
            {
                if (instruction.Param is int[] tableau)
                {
                    List<Label> listeLabels = new();
                    for (int i = 0; i < tableau.Length; i++)
                    {
                        cmd = retour.SingleOrDefault(c => c.offset == tableau[i]);
                        if (cmd == null)
                            throw new Exception("Un label n'a pas été trouvé");
                        else
                            listeLabels.Add(cmd.marqueLabel);
                    }
                    instruction.Param = listeLabels.ToArray();
                }
                else if (instruction.CodeIL.OperandType == OperandType.ShortInlineBrTarget)
                {
                    ILCommande cmdDestination = retour.SingleOrDefault(c => c.offset == int.Parse(instruction.Param.ToString()));
                    if (cmdDestination == null)
                        throw new Exception("Un label n'a pas été trouvé");
                    else
                    {
                        if (!cmdDestination.possedeLabel)
                            cmdDestination.MarqueDebutLabel(numLabel++);
                        instruction.Param = cmdDestination.marqueLabel;
                    }
                }
            }
            return retour;
        }
    }

    /*internal enum ECallConvention : byte
    {
        Default = 0,
        C = 1,
        StdCall = 2,
        ThisCall = 3,
        FastCall = 4,
        VarArg = 5,
        Generic = 10,
    }

    internal class ILCall
    {
        internal bool avecThis;
        internal byte call;
        internal ECallConvention ConventionAppel;
    }*/

    internal class ILCommande
    {
        internal int offset;
        internal OpCode CodeIL;
        internal byte ComplementCodeIL;
        internal object Param;
        internal Label marqueLabel;
        internal bool possedeLabel;

        internal void MarqueDebutLabel(int numLabel)
        {
            marqueLabel = (Label)typeof(Label).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0].Invoke(new object[] { numLabel });
            possedeLabel = true;
        }

        public void Emit(ILGenerator ilGen)
        {
            if (Param != null)
            {
                switch (Param)
                {
                    case byte b:
                        ilGen.Emit(CodeIL, b);
                        break;
                    case sbyte sb:
                        ilGen.Emit(CodeIL, sb);
                        break;
                    case Single s:
                        ilGen.Emit(CodeIL, s);
                        break;
                    case string s:
                        ilGen.Emit(CodeIL, s);
                        break;
                    case Int16 entierCourt:
                        ilGen.Emit(CodeIL, entierCourt);
                        break;
                    case Int32 entier:
                        ilGen.Emit(CodeIL, entier);
                        break;
                    case Int64 entierLong:
                        ilGen.Emit(CodeIL, entierLong);
                        break;
                    case double d:
                        ilGen.Emit(CodeIL, d);
                        break;
                    case FieldInfo fi:
                        ilGen.Emit(CodeIL, fi);
                        break;
                    case MethodInfo mi:
                        ilGen.Emit(CodeIL, mi);
                        break;
                    case ConstructorInfo ci:
                        ilGen.Emit(CodeIL, ci);
                        break;
                    case Type t:
                        ilGen.Emit(CodeIL, t);
                        break;
                    case Label[] listeLabels:
                        ilGen.Emit(CodeIL, listeLabels);
                        break;
                    case Label l:
                        ilGen.Emit(CodeIL, l);
                        break;
                    default:
                        throw new NotImplementedException($"Impossible d'écrire le code IL {CodeIL:G}, type Param={Param.GetType()}, Valeur Param={Param}");
                }
            }
            else
                ilGen.Emit(CodeIL);
        }
    }
}
