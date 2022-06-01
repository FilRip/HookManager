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
                if (code.Value == cmd)
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
                        cmd.Param = listeCodes.ReadInt32(ref offset);
                        break;
                    case OperandType.InlineString:
                        cmd.Param = methodeACopier.Module.ResolveString(listeCodes.ReadInt32(ref offset));
                        break;
                    case OperandType.InlineSwitch:
                        /*int taille = listeCodes.ReadInt32(ref offset);
                        int[] tableau = new int[taille];
                        for (int i = 0; i < taille; i++)
                        {
                            tableau[i] = listeCodes.ReadInt32(ref offset);
                        }
                        cmd.Param = tableau;*/
                        cmd.Param = listeCodes.ReadInt32(ref offset);
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
                }
            }
            else
                ilGen.Emit(CodeIL);
        }
    }
}
