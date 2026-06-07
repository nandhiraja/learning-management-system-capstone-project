namespace LMS.Core.Exception
{
    [System.Serializable]
    public class NotFoundException : System.Exception
    {
        public string EntityName { get; }
        public object Key { get; }
        
        public NotFoundException(string entityName, object key) 
            : base($"{entityName} with identifier '{key}' was not found.") 
        {
            EntityName = entityName;
            Key = key;
        }
    }
}