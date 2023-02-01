using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration.Json;

namespace ServiceWebG2_CallApp
{
    /*Le fichier Helper.cs regroupe l'ensemble des objets pouvant apporter une aide aux autres classes du projet*/


    /// <summary>
    /// Classe regroupant les méthodes d'aide générale
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Méthode permettant la récupération de toutes les exceptions imbriquées dans l'exception passée en paramètre
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static string AllInnerExceptions(Exception ex)
        {
            string result = string.Empty;
            if (ex != null)
            {
                while (ex.InnerException != null)
                {
                    if (result == string.Empty)
                        result = "Message : " + ex.Message + Environment.NewLine + "InnerException : " + ex.InnerException;
                    else
                        result += Environment.NewLine + "Message : " + ex.Message + Environment.NewLine + "InnerException : " + ex.InnerException;
                    ex = ex.InnerException;
                }
            }
            return result;
        }
    }

    /// <summary>
    /// Classe permettant d'acceder à la propriété Data du JsonConfigurationProvider qui est Protected
    /// </summary>
    public class JsonConfigurationProviderExtension : JsonConfigurationProvider
    {
        public JsonConfigurationProviderExtension(JsonConfigurationSource source) : base(source) { }

        public IDictionary<string, string> ProviderData
        {
            get
            {
                return base.Data;
            }
        }
    }
}
