using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace ServiceWebG2_CallApp
{
    public static class Logger
    {
        private static ConfigLogger _ConfigLogger
        {
            get { return Startup.Configuration.GetSection("Logger").Get<ConfigLogger>(); }
        }

        private static bool _IsActive
        {
            get { return _ConfigLogger.LogFilePath == string.Empty || _ConfigLogger.LogActivation; }
        }

        public static string LogFilePath
        {
            get { return _ConfigLogger.LogFilePath += Path.DirectorySeparatorChar + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_ServiceWebG2_CallApp.log"; }
        }

        public static bool UseDetailedLogs
        {
            get { return _ConfigLogger.DetailedLog; }
        }

        private static void Log(string category, string line)
        {
            if (!_IsActive)
                return;

            try
            {
                File.AppendAllText(LogFilePath, DateTime.Now.ToString("HH:mm:ss") + "\t" + category + "\t" + line + Environment.NewLine, Encoding.Unicode);
            }
            catch (Exception) { }
        }

        public static void Init()
        {
            if (!_IsActive)
                return;

            DumpEnvironmentInfo();
        }

        //Log des informations d'environnement et de configuration
        private static void DumpEnvironmentInfo()
        {
            Ligne();
            Env("#START_DT" + "\t" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Env("#MACHINE_NAME" + "\t" + (null == Environment.MachineName ? "" : Environment.MachineName.ToUpper()));
            Env("#USER_NAME" + "\t" + (null == Environment.UserName ? "" : Environment.UserName.ToUpper()));
            Env("#USER_DOMAIN" + "\t" + (null == Environment.UserDomainName ? "" : Environment.UserDomainName.ToUpper()));
            Env("#ASSEMBLY_VERSION" + "\t" + System.Reflection.Assembly.GetExecutingAssembly().FullName);
            Log("", "");
            Config("#Parametres de demarrage :");

            //Récupération des fichiers de config
            ConfigurationRoot root = (ConfigurationRoot)Startup.Configuration;
            foreach (IConfigurationProvider provider in root.Providers)
            {
                if (provider.GetType() == typeof(JsonConfigurationProvider))
                {
                    JsonConfigurationProvider jsonProvider = (JsonConfigurationProvider)provider;
                    if (jsonProvider.Source.Path == "appsettings.json" || jsonProvider.Source.Path == "appconfig.json")
                    {
                        Config("*******" + jsonProvider.Source.Path + "*******");
                        JsonConfigurationProviderExtension extendedProvider = new JsonConfigurationProviderExtension((JsonConfigurationSource)jsonProvider.Source);
                        extendedProvider.Load();

                        //Ecriture de ces infos dans le fichier de log
                        foreach (KeyValuePair<string, string> kvp in extendedProvider.ProviderData)
                        {
                            Config(kvp.Key + "\t" + kvp.Value);
                        }
                    }
                }
            }
        }

        private static void Env(string message)
        {
            Log("ENV", message);
        }

        private static void Config(string messge)
        {
            Log("CONFIG", messge);
        }

        public static void Ligne()
        {
            Log("----", "--------------------------------------------------------");
        }

        public static void Info(string message)
        {
            Log("INFO", message);
        }

        public static void Erreur(string message)
        {
            Log("ERREUR", message);
        }

        public static void Erreur(string message, Exception e)
        {
            Erreur(message);
            Erreur(e);
        }

        public static void Erreur(Exception e)
        {
            Log("ERREUR", e.GetType() + " - " + e.Message + " - " + e.StackTrace);
            if (UseDetailedLogs)
            {
                Erreur(Utils.AllInnerExceptions(e));
            }
        }
    }
}
