using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Autofac;
using Autofac.Core;
using Autofac.Features.Metadata;
using Microsoft.Practices.ServiceLocation;
using Prism.Autofac.Properties;
using Prism.Logging;
using IModule = Prism.Modularity.IModule;

namespace Prism.Autofac
{
    /// <summary>
    /// Defines a <see cref="IContainer"/> adapter for the <see cref="IServiceLocator"/> interface to be used by the Prism Library.
    /// </summary>
    public class AutofacServiceLocatorAdapter : ServiceLocatorImplBase
    {
        private readonly IContainer _container;
        private object resolvedTypeToReturn;
        Meta<IComponentRegistration, PrismAutofacMetadata> _componentRegistration;
        /// <summary>
        /// Initializes a new instance of <see cref="AutofacServiceLocatorAdapter"/>.
        /// </summary>
        /// <param name="container">The <see cref="IContainer"/> that will be used
        /// by the <see cref="DoGetInstance"/> and <see cref="DoGetAllInstances"/> methods.</param>
        public AutofacServiceLocatorAdapter(IContainer container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));
            _container = container;
        }

        /// <summary>
        /// Resolves the instance of the requested service.
        /// </summary>
        /// <param name="serviceType">Type of instance requested.</param>
        /// <param name="key">Name of registered service you want. May be null.</param>
        /// <returns>The requested service instance.</returns>
        protected override object DoGetInstance(Type serviceType, string key)
        {
            var isModule = serviceType.GetInterfaces().Any(t => t.IsGenericType &&
t.GetGenericTypeDefinition() == typeof(IModule));

            var metaDataDictionaries = _container.ComponentRegistry.Registrations.Where(reg => reg.Metadata.Count >= 1).Select(md => md.Metadata);
            var typeValues = metaDataDictionaries.SelectMany(md => md.Values);

            PrismLifetimeScope prismLifetimeScope = PrismLifetimeScope.PrismSingletonScoped;
            bool foundLifeTimeScope = false;
            //Debug.WriteLine("=============Searching for LifetimeScope=================");
            //Debug.WriteLine(String.Format(CultureInfo.CurrentCulture, "Searching for {0}", serviceType.Name));
            foreach (var item in typeValues)
            {
                var dto = item as PrismAutofacMetadataDTO;
                if (dto != null)
                {
                    if (dto.Type == serviceType)
                    {
                        prismLifetimeScope = dto.LifetimeScopeName;
                        foundLifeTimeScope = true;
                    }
                }
            }
            if (foundLifeTimeScope)
            {
                //Debug.WriteLine(String.Format(CultureInfo.CurrentCulture, "Found ServiceType {0} therefore using {1}", serviceType.Name, prismLifetimeScope.ToString()));
            }
            else {
                //Debug.WriteLine(String.Format(CultureInfo.CurrentCulture, "Did Not Find ServiceType {0} therefore using {1}", serviceType.Name, prismLifetimeScope.ToString()));
            }

            if (isModule)
            {
                prismLifetimeScope = PrismLifetimeScope.PrismSingletonScoped;
            }

            //Debug.WriteLine(String.Format(CultureInfo.CurrentCulture, Resources.ResolvingPrsimAutofacLifetimeScopedService, serviceType.Name, prismLifetimeScope.ToString()), Category.Debug, Priority.Low);

            resolvedTypeToReturn = null;
            if (key != null)
            {
                switch (prismLifetimeScope)
                {
                    case PrismLifetimeScope.PrismLifetimeScoped:
                        using (var scope = _container.BeginLifetimeScope(PrismAutofacMetadata.Prism))
                        {
                            resolvedTypeToReturn = scope.ResolveNamed(key, serviceType);
                        }
                        break;
                    case PrismLifetimeScope.PrismSingletonScoped:
                        resolvedTypeToReturn = _container.ResolveNamed(key, serviceType);
                        break;
                }

            }
            else {
                switch (prismLifetimeScope)
                {
                    case PrismLifetimeScope.PrismLifetimeScoped:
                        using (var scope = _container.BeginLifetimeScope(PrismAutofacMetadata.Prism))
                        {
                            resolvedTypeToReturn = scope.Resolve(serviceType);
                        }
                        break;
                    case PrismLifetimeScope.PrismSingletonScoped:
                        resolvedTypeToReturn = _container.Resolve(serviceType);
                        break;
                }
            }

            return resolvedTypeToReturn;
        }

        /// <summary>
        /// Resolves all the instances of the requested service.
        /// </summary>
        /// <param name="serviceType">Type of service requested.</param>
        /// <returns>Sequence of service instance objects.</returns>
        protected override IEnumerable<object> DoGetAllInstances(Type serviceType)
        {
            using (var scope = _container.BeginLifetimeScope(PrismAutofacMetadata.Prism))
            {
                var enumerableType = typeof(IEnumerable<>).MakeGenericType(serviceType);

                object instance = scope.Resolve(enumerableType);
                return ((IEnumerable)instance).Cast<object>();
            }
        }
    }
}
