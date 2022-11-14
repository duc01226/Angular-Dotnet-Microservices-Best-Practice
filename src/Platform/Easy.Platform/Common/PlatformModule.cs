using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Easy.Platform.Application;
using Easy.Platform.Common.Cqrs;
using Easy.Platform.Common.DependencyInjection;
using Easy.Platform.Common.Extensions;
using Easy.Platform.Common.JsonSerialization;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Easy.Platform.Common;

public interface IPlatformModule
{
    /// <summary>
    /// Higher Priority value mean the module init will be executed before lower Priority value in the same level module dependencies
    /// <br />
    /// Default is 1. For the default priority should be: PermissionModule => InfrastructureModule => Others Module
    /// </summary>
    public int ExecuteInitPriority { get; }

    public IServiceCollection ServiceCollection { get; }
    public IServiceProvider ServiceProvider { get; }
    public IConfiguration Configuration { get; }
    public bool IsDependencyModule { get; set; }
    public bool IsRootModule => CheckIsRootModule(this);

    /// <summary>
    /// Current runtime module instance Assembly
    /// </summary>
    public Assembly Assembly { get; }

    public bool RegisterServicesExecuted { get; }
    public bool Initiated { get; }

    public static bool CheckIsRootModule(IPlatformModule module)
    {
        return !module.IsDependencyModule;
    }

    /// <summary>
    /// Override this to call every time a new platform module is registered
    /// </summary>
    public void OnNewOtherModuleRegistered(
        IServiceCollection serviceCollection,
        PlatformModule newOtherRegisterModule);

    public void RegisterRuntimeModuleDependencies<TModule>(
        IServiceCollection serviceCollection) where TModule : PlatformModule;

    public void RegisterServices(IServiceCollection serviceCollection);

    public void Init();
}

/// <summary>
/// Example:
/// <br />
/// services.RegisterModule{XXXApiModule}(); Register module into service collection
/// <br />
/// get module service in collection and call module.Init();
/// Init module to start running init for all other modules and this module itself
/// </summary>
public abstract class PlatformModule : IPlatformModule
{
    public const int DefaultExecuteInitPriority = 1;

    protected static readonly ConcurrentDictionary<string, Assembly> ExecutedRegisterByAssemblies = new();

    protected readonly object InitLock = new();
    protected readonly object RegisterLock = new();

    public PlatformModule(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        ServiceProvider = serviceProvider;
        Configuration = configuration;
        Logger = serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    protected ILogger Logger { get; init; }

    public bool IsRootModule => IPlatformModule.CheckIsRootModule(this);

    /// <summary>
    /// Higher Priority value mean the module init will be executed before lower Priority value in the same level module dependencies
    /// <br />
    /// Default is 1. For the default priority should be: PermissionModule => InfrastructureModule => Others Module
    /// </summary>
    public virtual int ExecuteInitPriority => DefaultExecuteInitPriority;

    public IServiceCollection ServiceCollection { get; private set; }
    public IServiceProvider ServiceProvider { get; }
    public IConfiguration Configuration { get; }

    /// <summary>
    /// True if the module is in a dependency list of other module
    /// </summary>
    public bool IsDependencyModule { get; set; }

    /// <summary>
    /// Current runtime module instance Assembly
    /// </summary>
    public Assembly Assembly => GetType().Assembly;

    public bool RegisterServicesExecuted { get; protected set; }

    public bool Initiated { get; protected set; }

    /// <summary>
    /// Override this to call every time a new other module is registered
    /// </summary>
    public virtual void OnNewOtherModuleRegistered(
        IServiceCollection serviceCollection,
        PlatformModule newOtherRegisterModule)
    {
    }

    public void RegisterRuntimeModuleDependencies<TModule>(
        IServiceCollection serviceCollection) where TModule : PlatformModule
    {
        serviceCollection.RegisterModule<TModule>();
    }

    public void RegisterServices(IServiceCollection serviceCollection)
    {
        lock (RegisterLock)
        {
            if (RegisterServicesExecuted)
                return;

            ServiceCollection = serviceCollection;
            RegisterAllModuleDependencies(serviceCollection);
            RegisterDefaultLogs(serviceCollection);
            RegisterCqrs(serviceCollection);
            RegisterHelpers(serviceCollection);
            InternalRegister(serviceCollection);

            RegisterServicesExecuted = true;

            if (JsonSerializerCurrentOptions() != null)
                PlatformJsonSerializer.SetCurrentOptions(JsonSerializerCurrentOptions());
        }
    }

    public virtual void Init()
    {
        lock (InitLock)
        {
            if (Initiated)
                return;

            Logger.LogInformation("[PlatformModule] {module} start initiating", GetType().Name);

            // Because PlatformModule is singleton => ServiceProvider of it is the root ServiceProvider
            PlatformApplicationGlobal.RootServiceProvider = ServiceProvider;

            InitAllModuleDependencies();

            using (var scope = ServiceProvider.CreateScope())
            {
                InternalInit(scope).GetAwaiter().GetResult();
            }

            Initiated = true;

            Logger.LogInformation("[PlatformModule] {module} initiated", GetType().Name);
        }
    }

    protected static void ExecuteRegisterByAssemblyOnlyOnce(Action<Assembly> action, Assembly assembly, string actionName)
    {
        var executedRegisterByAssemblyKey = $"Action:{ExecutedRegisterByAssemblies.ContainsKey(actionName)};Assembly:{assembly.FullName}";

        if (!ExecutedRegisterByAssemblies.ContainsKey(executedRegisterByAssemblyKey))
        {
            action(assembly);

            ExecutedRegisterByAssemblies.TryAdd(executedRegisterByAssemblyKey, assembly);
        }
    }

    protected virtual void InternalRegister(IServiceCollection serviceCollection) { }

    protected virtual Task InternalInit(IServiceScope serviceScope)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Define list of any modules that this module depend on. The type must be assigned to <see cref="PlatformModule" />.
    /// Example from a XXXServiceAspNetCoreModule could depend on XXXPlatformApplicationModule and
    /// XXXPlatformPersistenceModule.
    /// Example code : return new { config => typeof(XXXPlatformApplicationModule), config =>
    /// typeof(XXXPlatformPersistenceModule) };
    /// </summary>
    protected virtual List<Func<IConfiguration, Type>> ModuleTypeDependencies()
    {
        return new List<Func<IConfiguration, Type>>();
    }

    /// <summary>
    /// Override this to setup custom value for <see cref="PlatformJsonSerializer.CurrentOptions" />
    /// </summary>
    /// <returns></returns>
    protected virtual JsonSerializerOptions JsonSerializerCurrentOptions()
    {
        return null;
    }

    protected void InitAllModuleDependencies()
    {
        ModuleTypeDependencies()
            .Select(
                moduleTypeProvider =>
                {
                    var moduleType = moduleTypeProvider(Configuration);

                    var dependModule = ServiceProvider.GetService(moduleType)
                        .As<IPlatformModule>()
                        .Ensure(
                            dependModule => dependModule != null,
                            $"Module {GetType().Name} depend on {moduleType.Name} but Module {moduleType.Name} does not implement IPlatformModule");

                    dependModule.IsDependencyModule = true;

                    return dependModule;
                })
            .OrderByDescending(p => p.ExecuteInitPriority)
            .ForEach(p => p.Init());
    }

    protected virtual void RegisterHelpers(IServiceCollection serviceCollection)
    {
        serviceCollection.RegisterAllFromType<IPlatformHelper>(Assembly);
    }

    private void RegisterDefaultLogs(IServiceCollection serviceCollection)
    {
        serviceCollection.RegisterIfServiceNotExist(typeof(ILoggerFactory), typeof(LoggerFactory));
        serviceCollection.RegisterIfServiceNotExist(typeof(ILogger<>), typeof(Logger<>));
    }

    private void RegisterCqrs(IServiceCollection serviceCollection)
    {
        ExecuteRegisterByAssemblyOnlyOnce(
            assembly =>
            {
                serviceCollection.AddMediatR(assembly);

                serviceCollection.Register<IPlatformCqrs, PlatformCqrs>();
                serviceCollection.RegisterAllFromType(conventionalType: typeof(IPipelineBehavior<,>), assembly);
            },
            Assembly,
            actionName: nameof(RegisterCqrs));
    }

    private void RegisterAllModuleDependencies(IServiceCollection serviceCollection)
    {
        ModuleTypeDependencies()
            .Select(moduleTypeProvider => moduleTypeProvider(Configuration))
            .ForEach(moduleType => serviceCollection.RegisterModule(moduleType));
    }
}