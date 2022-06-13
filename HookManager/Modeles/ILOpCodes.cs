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
            int offset;
            ILCommande cmd;
            for (offset = 0; offset < listeCodes.Length; offset++)
            {
                if (offset > 0)
                    offset--;
                OpCode commande = RetourneOpCode(listeCodes, ref offset, out byte complement);
                cmd = new();
                cmd.CodeIL = commande;
                cmd.ComplementCodeIL = complement;
                switch (commande.OperandType)
                {
                    case OperandType.InlineBrTarget:
                        int saut = listeCodes.ReadInt32(ref offset);
                        cmd.Param = offset + saut;
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
                        byte[] donnees = methodeACopier.Module.ResolveSignature(signature);
                        /*int offsetDonnees = 0;
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
                        cmd.Param = donnees;
                        break;
                    case OperandType.InlineString:
                        cmd.Param = methodeACopier.Module.ResolveString(listeCodes.ReadInt32(ref offset));
                        break;
                    case OperandType.InlineSwitch:
                        int taille = listeCodes.ReadInt32(ref offset);
                        int start = offset + (4 * taille);
                        int[] tableau = new int[taille];
                        for (int i = 0; i < taille; i++)
                        {
                            tableau[i] = listeCodes.ReadInt32(ref offset) + start;
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
                        cmd.Param = listeCodes[offset++];
                        break;
                    case OperandType.ShortInlineI:
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
        internal OpCode CodeIL;
        internal byte ComplementCodeIL;
        internal object Param;

        internal string NomTypeParam
        {
            get
            {
                if (Param != null)
                {
                    if (Param is FieldInfo)
                        return "fieldinfo";
                    if (Param is ConstructorInfo)
                        return "constructorinfo";
                    if (Param is MethodInfo)
                        return "methodinfo";
                    return Param.GetType().Name.ToLower();
                }
                else
                    return null;
            }
        }

        public void Emit(ILGenerator ilGen)
        {
            if (Param != null)
            {
                switch (NomTypeParam)
                {
                    case "byte":
                        ilGen.Emit(CodeIL, (byte)Param);
                        break;
                    case "single":
                        ilGen.Emit(CodeIL, (Single)Param);
                        break;
                    case "string":
                        ilGen.Emit(CodeIL, (string)Param);
                        break;
                    case "int16":
                        ilGen.Emit(CodeIL, (Int16)Param);
                        break;
                    case "int32":
                        ilGen.Emit(CodeIL, (Int32)Param);
                        break;
                    case "int64":
                        ilGen.Emit(CodeIL, (Int64)Param);
                        break;
                    case "double":
                        ilGen.Emit(CodeIL, (double)Param);
                        break;
                    case "fieldinfo":
                        ilGen.Emit(CodeIL, (FieldInfo)Param);
                        break;
                    case "methodinfo":
                        ilGen.Emit(CodeIL, (MethodInfo)Param);
                        break;
                    case "constructorinfo":
                        ilGen.Emit(CodeIL, (ConstructorInfo)Param);
                        break;
                    case "int[]":
                        // TODO
                        throw new NotImplementedException($"Commande InlineSwitch non implémentée encore.");
                    default:
                        throw new NotImplementedException($"Impossible d'écrire le code IL {CodeIL:G}, type Param={NomTypeParam}");
                }
            }
            else
                ilGen.Emit(CodeIL);
        }
    }
}
