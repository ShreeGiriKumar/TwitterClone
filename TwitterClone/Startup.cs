using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(TwitterClone.Startup))]
namespace TwitterClone
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
