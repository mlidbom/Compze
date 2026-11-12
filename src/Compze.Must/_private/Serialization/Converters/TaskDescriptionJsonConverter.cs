using Newtonsoft.Json;

namespace Compze.Must._private.Serialization.Converters;

/// <summary>
/// Writes a <see cref="Task"/> as its status, its result once it has one, and its exception's message if it faulted,
/// instead of serializing its members.
/// </summary>
/// <remarks>
/// Reading <see cref="Task{TResult}.Result"/> of a task that has not completed blocks until it does, which for a task
/// that never completes hung the test forever.
/// </remarks>
class TaskDescriptionJsonConverter : WriteOnlyJsonConverter
{
   public override bool CanConvert(Type objectType) => typeof(Task).IsAssignableFrom(objectType);

   public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
   {
      var task = (Task)value!;
      writer.WriteStartObject();

      if(serializer.TypeNameHandling != TypeNameHandling.None)
      {
         writer.WritePropertyName("$type");
         writer.WriteValue($"{task.GetType().FullName}, {task.GetType().Assembly.GetName().Name}");
      }

      writer.WritePropertyName(nameof(Task.Status));
      writer.WriteValue(task.Status.ToString());

      if(task.Status == TaskStatus.RanToCompletion && HasAResult(task))
      {
         writer.WritePropertyName(nameof(Task<>.Result));
         serializer.Serialize(writer, task.GetType().GetProperty(nameof(Task<>.Result))!.GetValue(task));
      }

      if(task.Exception is {} exception)
      {
         writer.WritePropertyName(nameof(Task.Exception));
         writer.WriteValue(exception.Message);
      }

      writer.WriteEndObject();
   }

   static bool HasAResult(Task task) => task.GetType().IsGenericType && task.GetType().GetProperty(nameof(Task<>.Result)) != null;
}
