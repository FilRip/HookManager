using System;
using System.Runtime.Serialization;

namespace HookManager.Exceptions
{
    /// <summary>
    /// Erreur, la "nouvelle" méthode de remplacement doit avoir un premier paramètre du même type que l'objet contenant la méthode remplacée (ou un type héritant), et en plus : au moins le même nombre de paramètre que la méthode qu'elle remplace (elle peut en avoir plus sans problèmes)<br/>
    /// Si la méthode remplacée est static, il n'y a pas besoin du premier paramètre (type de l'objet remplacé) puis ce qu'une static n'a pas d'instance d'objet lié.
    /// </summary>
    [Serializable()]
    public class MissingDefaultArgumentException : HookManagerException
    {
        private readonly string _nomMethode;
        private readonly bool _gac;

        /// <summary>
        /// Erreur, la "nouvelle" méthode de remplacement doit avoir un premier paramètre du même type que l'objet contenant la méthode remplacée, et en plus : au moins le même nombre de paramètre que la méthode qu'elle remplace<br/>
        /// Si la méthode remplacée est static, il ne faut pas le premier paramètre (type de l'objet remplacé) puis ce qu'une static n'a pas d'instance d'objet lié.
        /// </summary>
        /// <param name="nomMethode">Nom de la méthode de remplacement</param>
        /// <param name="GAC">Est-ce le remplacement d'une méthode d'un type dans le GAC (Global Assembly Cache)</param>
        internal MissingDefaultArgumentException(string nomMethode, bool GAC = false) : base()
        {
            _nomMethode = nomMethode;
            _gac = GAC;
        }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return $"La méthode {_nomMethode} doit obligatoirement avoir un premier paramètre du même type que l'objet de l'instance source (ou 'object' pour une méthode générique), puis les paramètres de la méthode source, si il y en a." + Environment.NewLine
                    + "Si la méthode source est static, il faut uniquement les paramètres de la méthode source, si il y en a";
            }
        }

        /// <summary>
        /// Nom de la méthode de remplacement
        /// </summary>
        public string NomMethode
        {
            get
            {
                return _nomMethode;
            }
        }

        /// <summary>
        /// Est-ce le remplacement d'une méthode d'un type dans le GAC (Global Assembly Cache)
        /// </summary>
        public bool IsGAC
        {
            get
            {
                return _gac;
            }
        }

        /// <inheritdoc/>
        protected MissingDefaultArgumentException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
