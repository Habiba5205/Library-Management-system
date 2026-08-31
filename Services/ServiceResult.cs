namespace Lib_System.Services
{
    // Shared by services across the app - not Borrowing-specific.
    // Lets a service report success/failure plus which form field(s)
    // an error belongs to, without depending on ModelState or MVC types.
    public class ServiceResult
    {
        public List<(string Field, string Message)> Errors { get; } = new();

        public bool Success => Errors.Count == 0;

        public void AddError(string field, string message)
        {
            Errors.Add((field, message));
        }

        // Use an empty field name for a general/non-field-specific error
        // (maps to ModelState.AddModelError(string.Empty, message) in the controller).
        public void AddError(string message) => AddError(string.Empty, message);
    }
}
