// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.ModelBinding.Internal;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class ModelBinderFactoryTest
    {
        // No providers => can't create a binder
        [Fact]
        public void CreateBinder_Throws_WhenBinderNotCreated()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            var options = new TestOptionsManager<MvcOptions>();
            var factory = new ModelBinderFactory(metadataProvider, options);

            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForType(typeof(string)),
            };

            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateBinder(context));

            // Assert
            Assert.Equal(
                $"Could not create a model binder for model object of type '{typeof(string).FullName}'.",
                exception.Message);
        }

        [Fact]
        public void CreateBinder_CreatesNoOpBinder_WhenPropertyDoesntHaveABinder()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();

            // There isn't a provider that can handle WidgetId.
            var options = new TestOptionsManager<MvcOptions>();
            options.Value.ModelBinderProviders.Add(new TestModelBinderProvider(c =>
            {
                if (c.Metadata.ModelType == typeof(Widget))
                {
                    Assert.NotNull(c.CreateBinder(c.Metadata.Properties[nameof(Widget.Id)]));
                    return Mock.Of<IModelBinder>();
                }

                return null;
            }));

            var factory = new ModelBinderFactory(metadataProvider, options);

            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForType(typeof(Widget)),
            };

            // Act
            var result = factory.CreateBinder(context);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateBinder_CreatesNoOpBinder_WhenPropertyBindingIsNotAllowed()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();
            metadataProvider
                .ForProperty<Widget>(nameof(Widget.Id))
                .BindingDetails(m => m.IsBindingAllowed = false);

            var modelBinder = new ByteArrayModelBinder();

            var options = new TestOptionsManager<MvcOptions>();
            options.Value.ModelBinderProviders.Add(new TestModelBinderProvider(c =>
            {
                if (c.Metadata.ModelType == typeof(WidgetId))
                {
                    return modelBinder;
                }

                return null;
            }));

            var factory = new ModelBinderFactory(metadataProvider, options);

            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForProperty(typeof(Widget), nameof(Widget.Id)),
            };

            // Act
            var result = factory.CreateBinder(context);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<NoOpBinder>(result);
        }

        [Fact]
        public void CreateBinder_NestedProperties()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();

            var options = new TestOptionsManager<MvcOptions>();
            options.Value.ModelBinderProviders.Add(new TestModelBinderProvider(c =>
            {
                if (c.Metadata.ModelType == typeof(Widget))
                {
                    Assert.NotNull(c.CreateBinder(c.Metadata.Properties[nameof(Widget.Id)]));
                    return Mock.Of<IModelBinder>();
                }
                else if (c.Metadata.ModelType == typeof(WidgetId))
                {
                    return Mock.Of<IModelBinder>();
                }

                return null;
            }));

            var factory = new ModelBinderFactory(metadataProvider, options);

            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForType(typeof(Widget)),
            };

            // Act
            var result = factory.CreateBinder(context);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateBinder_BreaksCycles()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();

            var callCount = 0;

            var options = new TestOptionsManager<MvcOptions>();
            options.Value.ModelBinderProviders.Add(new TestModelBinderProvider(c =>
            {
                var currentCallCount = ++callCount;
                Assert.Equal(typeof(Employee), c.Metadata.ModelType);
                var binder = c.CreateBinder(c.Metadata.Properties[nameof(Employee.Manager)]);

                if (currentCallCount == 2)
                {
                    Assert.IsType<PlaceholderBinder>(binder);
                }

                return Mock.Of<IModelBinder>();
            }));

            var factory = new ModelBinderFactory(metadataProvider, options);

            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForType(typeof(Employee)),
            };

            // Act
            var result = factory.CreateBinder(context);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void CreateBinder_DoesNotCache_WhenTokenIsNull()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();

            var options = new TestOptionsManager<MvcOptions>();
            options.Value.ModelBinderProviders.Add(new TestModelBinderProvider(c =>
            {
                Assert.Equal(typeof(Employee), c.Metadata.ModelType);
                return Mock.Of<IModelBinder>();
            }));

            var factory = new ModelBinderFactory(metadataProvider, options);

            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForType(typeof(Employee)),
            };

            // Act
            var result1 = factory.CreateBinder(context);
            var result2 = factory.CreateBinder(context);

            // Assert
            Assert.NotSame(result1, result2);
        }

        [Fact]
        public void CreateBinder_Caches_WhenTokenIsNotNull()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();

            var options = new TestOptionsManager<MvcOptions>();
            options.Value.ModelBinderProviders.Add(new TestModelBinderProvider(c =>
            {
                Assert.Equal(typeof(Employee), c.Metadata.ModelType);
                return Mock.Of<IModelBinder>();
            }));

            var factory = new ModelBinderFactory(metadataProvider, options);

            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForType(typeof(Employee)),
                CacheToken = new object(),
            };

            // Act
            var result1 = factory.CreateBinder(context);
            var result2 = factory.CreateBinder(context);

            // Assert
            Assert.Same(result1, result2);
        }

        [Fact]
        public void CreateBinder_Caches_InteriorNodes()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();

            var options = new TestOptionsManager<MvcOptions>();

            IModelBinder inner = null;

            var widgetProvider = new TestModelBinderProvider(c =>
            {
                if (c.Metadata.ModelType == typeof(Widget))
                {
                    var binder = c.CreateBinder(c.Metadata.Properties[nameof(Widget.Id)]);
                    if (inner == null)
                    {
                        inner = binder;
                    }
                    else
                    {
                        Assert.Same(inner, binder);
                    }

                    return Mock.Of<IModelBinder>();
                }

                return null;
            });

            var widgetIdProvider = new TestModelBinderProvider(c =>
            {
                Assert.Equal(typeof(WidgetId), c.Metadata.ModelType);
                return Mock.Of<IModelBinder>();
            });

            options.Value.ModelBinderProviders.Add(widgetProvider);
            options.Value.ModelBinderProviders.Add(widgetIdProvider);

            var factory = new ModelBinderFactory(metadataProvider, options);

            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForType(typeof(Widget)),
                CacheToken = null, // We want the provider to run twice.
            };

            // Act
            var result1 = factory.CreateBinder(context);
            var result2 = factory.CreateBinder(context);

            // Assert
            Assert.NotSame(result1, result2);

            Assert.Equal(2, widgetProvider.SuccessCount);
            Assert.Equal(1, widgetIdProvider.SuccessCount);
        }

        [Fact]
        public void CreateBinder_Caches_InteriorNodes_WhenInteriorNodeReturnsNull()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();

            var options = new TestOptionsManager<MvcOptions>();

            IModelBinder inner = null;

            var widgetProvider = new TestModelBinderProvider(c =>
            {
                if (c.Metadata.ModelType == typeof(Widget))
                {
                    var binder = c.CreateBinder(c.Metadata.Properties[nameof(Widget.Id)]);
                    Assert.IsType<NoOpBinder>(binder);
                    if (inner == null)
                    {
                        inner = binder;
                    }
                    else
                    {
                        Assert.Same(inner, binder);
                    }

                    return Mock.Of<IModelBinder>();
                }

                return null;
            });

            var widgetIdProvider = new TestModelBinderProvider(c =>
            {
                Assert.Equal(typeof(WidgetId), c.Metadata.ModelType);
                return null;
            });

            options.Value.ModelBinderProviders.Add(widgetProvider);
            options.Value.ModelBinderProviders.Add(widgetIdProvider);

            var factory = new ModelBinderFactory(metadataProvider, options);

            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForType(typeof(Widget)),
                CacheToken = null, // We want the provider to run twice.
            };

            // Act
            var result1 = factory.CreateBinder(context);
            var result2 = factory.CreateBinder(context);

            // Assert
            Assert.NotSame(result1, result2);

            Assert.Equal(2, widgetProvider.SuccessCount);
            Assert.Equal(0, widgetIdProvider.SuccessCount);
        }

        // The fact that we use the ModelMetadata as the token is important for caching
        // and sharing with TryUpdateModel.
        [Fact]
        public void CreateBinder_Caches_InteriorNodes_UsesModelMetadataAsToken()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();

            var options = new TestOptionsManager<MvcOptions>();

            IModelBinder inner = null;

            var widgetProvider = new TestModelBinderProvider(c =>
            {
                if (c.Metadata.ModelType == typeof(Widget))
                {
                    inner = c.CreateBinder(c.Metadata.Properties[nameof(Widget.Id)]);
                    return Mock.Of<IModelBinder>();
                }

                return null;
            });

            var widgetIdProvider = new TestModelBinderProvider(c =>
            {
                Assert.Equal(typeof(WidgetId), c.Metadata.ModelType);
                return Mock.Of<IModelBinder>();
            });

            options.Value.ModelBinderProviders.Add(widgetProvider);
            options.Value.ModelBinderProviders.Add(widgetIdProvider);

            var factory = new ModelBinderFactory(metadataProvider, options);

            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForType(typeof(Widget)),
                CacheToken = null,
            };

            // Act 1
            var result1 = factory.CreateBinder(context);

            context.Metadata = context.Metadata.Properties[nameof(Widget.Id)];
            context.CacheToken = context.Metadata;

            // Act 2
            var result2 = factory.CreateBinder(context);

            // Assert
            Assert.Same(inner, result2);
            Assert.Equal(1, widgetProvider.SuccessCount);
            Assert.Equal(1, widgetIdProvider.SuccessCount);
        }

        // This is a really wierd case, but I wanted to make sure it's covered so it doesn't
        // blow up in wierd ways.
        //
        // If a binder provider tries to recursively create itself, but then returns null, we've
        // already returned and possibly cached the DelegatingBinder instance, we want to make sure that
        // instance won't nullref.
        [Fact]
        public void CreateBinder_Caches_InteriorNodes_FixesUpDelegatingBinder()
        {
            // Arrange
            var metadataProvider = new TestModelMetadataProvider();

            var options = new TestOptionsManager<MvcOptions>();

            IModelBinder inner = null;
            IModelBinder innerInner = null;

            var widgetProvider = new TestModelBinderProvider(c =>
            {
                if (c.Metadata.ModelType == typeof(Widget))
                {
                    inner = c.CreateBinder(c.Metadata.Properties[nameof(Widget.Id)]);
                    return Mock.Of<IModelBinder>();
                }

                return null;
            });

            var widgetIdProvider = new TestModelBinderProvider(c =>
            {
                Assert.Equal(typeof(WidgetId), c.Metadata.ModelType);
                innerInner = c.CreateBinder(c.Metadata);
                return null;
            });

            options.Value.ModelBinderProviders.Add(widgetProvider);
            options.Value.ModelBinderProviders.Add(widgetIdProvider);

            var factory = new ModelBinderFactory(metadataProvider, options);

            var context = new ModelBinderFactoryContext()
            {
                Metadata = metadataProvider.GetMetadataForType(typeof(Widget)),
                CacheToken = null,
            };

            // Act 1
            var result1 = factory.CreateBinder(context);

            context.Metadata = context.Metadata.Properties[nameof(Widget.Id)];
            context.CacheToken = context.Metadata;

            // Act 2
            var result2 = factory.CreateBinder(context);

            // Assert
            Assert.Same(inner, result2);
            Assert.NotSame(inner, innerInner);

            var placeholder = Assert.IsType<PlaceholderBinder>(innerInner);
            Assert.IsType<NoOpBinder>(placeholder.Inner);

            Assert.Equal(1, widgetProvider.SuccessCount);
            Assert.Equal(0, widgetIdProvider.SuccessCount);
        }

        private class Widget
        {
            public WidgetId Id { get; set; }
        }

        private class WidgetId
        {
        }

        private class Employee
        {
            public Employee Manager { get; set; }
        }

        private class TestModelBinderProvider : IModelBinderProvider
        {
            private readonly Func<ModelBinderProviderContext, IModelBinder> _factory;

            public TestModelBinderProvider(Func<ModelBinderProviderContext, IModelBinder> factory)
            {
                _factory = factory;
            }

            public int SuccessCount { get; private set; }

            public IModelBinder GetBinder(ModelBinderProviderContext context)
            {
                var binder = _factory(context);
                if (binder != null)
                {
                    SuccessCount++;
                }

                return binder;
            }
        }
    }
}
