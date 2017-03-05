using GreatSnooper.ServiceInterfaces;
using GreatSnooper.Services;
using SimpleInjector;
using SimpleInjector.Extensions.LifetimeScoping;

namespace GreatSnooper
{
    public class DI
    {
        private static Container _container;

        public DI()
        {
        }

        public TService Resolve<TService>()
            where TService : class
        {
            return _container.GetInstance<TService>();
        }

        public static void Init()
        {
            _container = new Container();
            _container.Options.DefaultLifestyle = Lifestyle.Scoped;
            _container.Options.DefaultScopedLifestyle = new LifetimeScopeLifestyle();

            _container.RegisterSingleton<DI>();
            _container.RegisterSingleton<IWormNetCharTable, WormNetCharTable>();

            _container.Verify();
        }
    }
}
