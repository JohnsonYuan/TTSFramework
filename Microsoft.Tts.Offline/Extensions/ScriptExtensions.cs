//----------------------------------------------------------------------------
// <copyright file="ScriptExtensions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <summary>
//     This module implements ScriptExtensions class.
// </summary>
//----------------------------------------------------------------------------

namespace Microsoft.Tts.Offline.Extensions
{
    using Microsoft.Tts.Offline.Utility;

    /// <summary>
    /// Script extensions.
    /// </summary>
    public static class ScriptExtensions
    {
        private const string ScriptIdFormat = "{0:D10}";

        /// <summary>
        /// Check whether the sentence type is question.
        /// </summary>
        /// <param name="type">Sentence type.</param>
        /// <returns>Whether the sentence type is question.</returns>
        public static bool IsQuestion(this SentenceType type)
        {
            return type == SentenceType.YesNoQuestion ||
                type == SentenceType.SingleWordQuestion ||
                type == SentenceType.ChoiceQuestion ||
                type == SentenceType.WhoQuestion;
        }

        /// <summary>
        /// Convert long to script item ID.
        /// </summary>
        /// <param name="idLongValue">ID long value.</param>
        /// <returns>Script item ID.</returns>
        public static string ToItemId(this long idLongValue)
        {
            return Helper.NeutralFormat(ScriptIdFormat, idLongValue);
        }

        /// <summary>
        /// Convert int to script item ID.
        /// </summary>
        /// <param name="idIntValue">ID int value.</param>
        /// <returns>Script item ID.</returns>
        public static string ToItemId(this int idIntValue)
        {
            return Helper.NeutralFormat(ScriptIdFormat, idIntValue);
        }
    }
}