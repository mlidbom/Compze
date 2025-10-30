using System;
using System.Globalization;
using Compze.Core.Time.Public;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;

namespace Compze.Core.Time.Testing.Public;

static class TestingTimeSourceRegistrar
{
   internal static IComponentRegistrar TestingTimeSource(this IComponentRegistrar registrar)
      => Public.TestingTimeSource.RegisterWith(registrar);
}

/// <summary> Just statically returns whatever value was assigned.</summary>
public class TestingTimeSource : IUtcTimeTimeSource
{
   internal static IComponentRegistrar RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(Singleton.For<IUtcTimeTimeSource, TestingTimeSource>()
                                  .CreatedBy(() => new TestingTimeSource())
                                  .DelegateToParentServiceLocatorWhenCloning());

   DateTime? _freezeAt;

   TestingTimeSource() {}

   ///<summary>Returns a time source that will continually return the time that it was created at as the current time.</summary>
   public static TestingTimeSource FrozenUtcNow() => new()
                                                     {
                                                        _freezeAt = DateTime.UtcNow
                                                     };

   ///<summary>Returns a time source that will forever return <param name="utcTime"> as the current time.</param></summary>
   public static TestingTimeSource FrozenAtUtcTime(DateTime utcTime) => new()
                                                                        {
                                                                           _freezeAt = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc)
                                                                        };

   public static TestingTimeSource FrozenAtUtcTime(string time) => FrozenAtUtcTime(DateTime.Parse(time, CultureInfo.InvariantCulture).ToUniversalTime());

   public void FreezeAtUtcTime(DateTime time) => _freezeAt = time.ToUniversalTimeSafely();

   public void FreezeAtUtcTime(string time) => FreezeAtUtcTime(DateTime.Parse(time, CultureInfo.InvariantCulture).ToUniversalTime());

   ///<summary>Gets the current UTC time.</summary>
   public DateTime UtcNow => _freezeAt ?? DateTime.UtcNow;
}
