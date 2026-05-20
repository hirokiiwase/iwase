using System;
using Microsoft.Xrm.Sdk;

namespace D365.Plugins
{
    /// <summary>
    /// Abstract base class that every plugin in this project should extend.
    /// Provides consistent error handling and a typed execution context.
    /// </summary>
    public abstract class PluginBase : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            var context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            var tracingService = (ITracingService)
                serviceProvider.GetService(typeof(ITracingService));

            var serviceFactory = (IOrganizationServiceFactory)
                serviceProvider.GetService(typeof(IOrganizationServiceFactory));

            var orgService = serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                ExecutePlugin(new PluginContext(context, tracingService, orgService));
            }
            catch (InvalidPluginExecutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                tracingService.Trace("Unhandled exception: {0}", ex.ToString());
                throw new InvalidPluginExecutionException(
                    $"An unexpected error occurred in {GetType().Name}: {ex.Message}", ex);
            }
        }

        protected abstract void ExecutePlugin(PluginContext ctx);
    }

    /// <summary>
    /// Wraps the three most-used services into a single convenience object.
    /// </summary>
    public sealed class PluginContext
    {
        public IPluginExecutionContext    ExecutionContext { get; }
        public ITracingService            Tracing         { get; }
        public IOrganizationService       OrgService      { get; }

        public PluginContext(
            IPluginExecutionContext context,
            ITracingService tracing,
            IOrganizationService orgService)
        {
            ExecutionContext = context;
            Tracing         = tracing;
            OrgService      = orgService;
        }

        /// <summary>Returns the primary entity from InputParameters["Target"].</summary>
        public Entity GetTargetEntity()
        {
            if (ExecutionContext.InputParameters.TryGetValue("Target", out var target) &&
                target is Entity entity)
            {
                return entity;
            }
            throw new InvalidPluginExecutionException("Target entity not found in InputParameters.");
        }
    }
}
