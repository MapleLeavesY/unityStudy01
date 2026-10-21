using VContainer;
using VContainer.Unity;

namespace Core {

    public class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            foreach (var entry in Register.All)
            {
                if(entry == null) continue;
                var type = entry.classType;
                builder.Register(type, Lifetime.Singleton);
            }
            
            builder.RegisterComponentInHierarchy<gameOpen>();
        }
    }
}