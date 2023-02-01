namespace ServiceWebG2_CallApp
{
    //Ensemble de classes permettant de lire le fichier de config appsettings.json
    //Le nom de Properties doivent être identique aux noms des clés du fichier de config

    public class AppDefinition
    {
        public string ContextArgs { get; set; }
        public CustomProcessStartInfo CustomProcessStartInfo { get; set; }
        public int? TimeoutMs { get; set; }
        public string RedirectionPath { get; set; }
    }

    public class CustomProcessStartInfo
    {
        public string FileName { get; set; }
        public bool UseShellExecute { get; set; }
        public string WorkingDirectory { get; set; }
        public string Arguments { get; set; }
        public bool CreateNoWindow { get; set; }
        public bool RedirectStandardError { get; set; }
        public bool RedirectStandardOutput { get; set; }
    }
}
