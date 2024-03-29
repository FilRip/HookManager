﻿using System;

namespace HookManager.Exceptions
{
    /// <summary>
    /// Erreur, la méthode que vous tentez de remplacer est déjà remplacée, consultez HookPool pour la liste
    /// </summary>
    [Serializable()]
    public class MethodAlreadyHooked : HookManagerException
    {
        private readonly string _nomMethode;

        /// <summary>
        /// Erreur, la méthode que vous tentez de remplacer est déjà remplacée, consultez HookPool pour la liste
        /// </summary>
        internal MethodAlreadyHooked(string nomMethode) : base()
        {
            _nomMethode = nomMethode;
        }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                return $"Cette méthode ({_nomMethode}) est déjà substituée. On ne peut substituer qu'une seule fois chaque méthode.";
            }
        }

        /// <summary>
        /// Nom de la méthode en cause
        /// </summary>
        public string NomMethode
        {
            get { return _nomMethode; }
        }
    }
}
