namespace Compze.DependencyInjection.Wiring.Registration;

/// <summary>
/// The fluent modifiers available on every <see cref="ComponentRegistrationWithoutInstantiationSpec"/> regardless of lifestyle.
/// Generic extensions returning the concrete spec type, so the chain keeps its full fluent surface after a modifier call.
/// </summary>
public static class ComponentRegistrationWithoutInstantiationSpecAssociatedRegistrationsExtensions
{
   extension<TSpec>(TSpec @this) where TSpec : ComponentRegistrationWithoutInstantiationSpec
   {
      /// <summary>
      /// Attaches extra registrations to be added to the container alongside this one when it is built — the general extension
      /// point described on <see cref="ComponentRegistration.AssociatedRegistrations"/>.
      /// </summary>
      /// <remarks>
      /// It is public so consumers of the library can write their own feature extensions on top of it without the core needing a
      /// dedicated method for each.
      /// </remarks>
      public TSpec WithAssociatedRegistrations(params ComponentRegistration[] registrations)
      {
         @this.AddAssociatedRegistrations(registrations);
         return @this;
      }

      /// <summary>
      /// Deferred variant: <paramref name="createAssociatedRegistrations"/> runs when the chain's terminal
      /// (<c>CreatedBy(...)</c>/<c>Instance(...)</c>) has built the finished <see cref="ComponentRegistration"/>, and receives it so
      /// the associated registrations can be derived from its service types, lifestyle, and policies.
      /// </summary>
      /// <remarks>
      /// Use this whenever the associated registrations depend on this registration itself — deferring until the terminal keeps
      /// them correct no matter where in the chain this call appears relative to other modifiers.<br/>
      /// This is what feature extensions such as <see cref="ComponentRegistrationWithoutInstantiationSpecServiceResolverExtensions"/>'s <c>WithServiceResolver()</c> build on.
      /// </remarks>
      internal TSpec WithAssociatedRegistrations(Func<ComponentRegistration, IEnumerable<ComponentRegistration>> createAssociatedRegistrations)
      {
         @this.AddAssociatedRegistrationsCreatedWhenBuilt(createAssociatedRegistrations);
         return @this;
      }
   }
}
