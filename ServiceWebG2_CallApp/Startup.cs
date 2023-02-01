using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceWebG2_CallApp
{
    public class Startup
    {
        public static IConfiguration Configuration { get; private set; }

        //Le paramètre IConfiguration est fourni au lancement de l'application. 
        //Il contient un ensemble de Providers correspondant aux différentes sources de configuration de l'application
        //On y retrouve par exemple le appsettings.json et les variables d'environnement
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Run(async (context) =>
            {
                //Récupérer en débug depuis Encoding.GetEncodings() :
                //Western European (ISO) => CodePage = 28591
                Encoding encoding = Encoding.GetEncoding(28591);

                try
                {
                    //Initalisation
                    Logger.Init();

                    //Récupération de la section
                    string appSettingsSectionName = context.Request.Path.ToString().Substring(context.Request.Path.ToString().LastIndexOf('/') + 1);
                    IConfigurationSection settingsSection = null;
                    AppDefinition configValues = null;
                    bool sectionExist = !string.IsNullOrWhiteSpace(appSettingsSectionName);
                    if (sectionExist)
                    {
                        try
                        {
                            settingsSection = Configuration.GetSection("AppToCall").GetSection(appSettingsSectionName);
                            configValues = settingsSection.Get<AppDefinition>();
                            if (configValues == null)
                                sectionExist = false;
                        }
                        catch
                        {
                            sectionExist = false;
                        }
                    }

                    if (sectionExist)
                    {


                        //Construction des paramètres supplémentaires à passer à l'exécutable
                        string additionalArgs = string.Empty;
                        if (context.Request.QueryString.HasValue)
                        {
                            Logger.Info(context.Request.QueryString.Value);

                            List<string> contextArgs = configValues.ContextArgs.Split(';').ToList();
                            foreach (string arg in contextArgs)
                                if (context.Request.Query.ContainsKey(arg))
                                    additionalArgs = string.Concat(additionalArgs, string.Format("{0}={1};", arg, context.Request.Query[arg]));

                            //Retrait du dernier ";"
                            if (!string.IsNullOrWhiteSpace(additionalArgs))
                            {
                                additionalArgs = additionalArgs.Substring(0, additionalArgs.Length - 1);
                                additionalArgs = "\"" + additionalArgs + "\"";
                            }
                        }

                        //Execution du programme
                        Task task = AppToCall.CallAppFromConfig(appSettingsSectionName, configValues, additionalArgs);
                        context.Response.StatusCode = 200;
                        await context.Response.WriteAsync("Succès", encoding);
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("Le programme '" + context.Request.Path.ToString().Substring(1) + "' n'est pas définit dans la configuration du Web Service.", encoding);
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("Erreur : " + ex.ToString(), encoding);
                    Logger.Erreur("Une erreur inattendue est survenue", ex);
                }
            });
        }
    }
}
