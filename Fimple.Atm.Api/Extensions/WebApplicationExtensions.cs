namespace Fimple.Atm.Api.Extensions
{
    public static class WebApplicationExtensions
    {
        private const string USE_HTTPS_REDIRECTION_KEY = "Hosting:UseHttpsRedirection";

        public static WebApplication UseHttpsRedirectionWhenEnabled(this WebApplication app)
        {
            var isEnabled = app.Configuration.GetValue(USE_HTTPS_REDIRECTION_KEY, true);
            if (isEnabled)
            {
                app.UseHttpsRedirection();
            }

            return app;
        }
    }
}
