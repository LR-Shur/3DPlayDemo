namespace Train.Architecture.Services
{
    /// <summary>
    /// Service storage for the composition root.
    /// Gameplay objects should receive interfaces through Initialize methods
    /// instead of resolving this registry themselves.
    /// </summary>
    public interface IServiceRegistry
    {
        void Install<TService>(TService service)
            where TService : class;

        bool TryResolve<TService>(out TService service)
            where TService : class;

        TService Resolve<TService>()
            where TService : class;
    }
}
