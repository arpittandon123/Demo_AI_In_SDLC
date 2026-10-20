using System.Reflection;
using InvoiceApp.Domain;
using Xunit;

namespace InvoiceApp.Tests;

public class ArchitectureTests
{
    [Fact]
    public void Domain_Should_Not_Reference_Api_Assembly()
    {
        var domainAssembly = typeof(Invoice).Assembly;

        var referencedAssemblies = domainAssembly.GetReferencedAssemblies();

        Assert.DoesNotContain(referencedAssemblies, assembly => assembly.Name == "InvoiceApp.Api");
    }

    [Fact]
    public void Controllers_Should_Not_Reference_Concrete_Repository_Classes()
    {
        var apiAssembly = typeof(InvoiceApp.Api.Controllers.InvoicesController).Assembly;
        var repositoryInterfaceType = typeof(IInvoiceRepository);
        var concreteRepositoryTypes = apiAssembly
            .GetTypes()
            .Where(type => type != repositoryInterfaceType && repositoryInterfaceType.IsAssignableFrom(type))
            .ToList();

        var controllerTypes = apiAssembly
            .GetTypes()
            .Where(type => type.Namespace == "InvoiceApp.Api.Controllers" && type.IsClass)
            .ToList();

        var violations = controllerTypes
            .SelectMany(type =>
            {
                var constructorParameters = type
                    .GetConstructors()
                    .SelectMany(ctor => ctor.GetParameters())
                    .Where(parameter => concreteRepositoryTypes.Contains(parameter.ParameterType))
                    .Select(parameter => $"{type.FullName} constructor parameter '{parameter.Name}' is typed as {parameter.ParameterType.FullName}");

                var fieldTypes = type
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(field => concreteRepositoryTypes.Contains(field.FieldType))
                    .Select(field => $"{type.FullName} field '{field.Name}' is typed as {field.FieldType.FullName}");

                var propertyTypes = type
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(property => concreteRepositoryTypes.Contains(property.PropertyType))
                    .Select(property => $"{type.FullName} property '{property.Name}' is typed as {property.PropertyType.FullName}");

                return constructorParameters.Concat(fieldTypes).Concat(propertyTypes);
            })
            .ToList();

        Assert.True(
            violations.Count == 0,
            "Controllers must depend on repository interfaces, not concrete repository classes. Violations: " + string.Join("; ", violations));
    }
}
