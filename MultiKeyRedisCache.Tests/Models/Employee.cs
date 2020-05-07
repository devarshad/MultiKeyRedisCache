namespace MultiKeyRedisCache.Tests.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return $"id: {Id}, name: {Name}";
        }
        public override bool Equals(object obj)
        {
            return this.Equals(obj as Employee);
        }

        public bool Equals(Employee other)
        {
            return other != null
                &&
            this.Id == other.Id
                &&
            this.Name.Equals(other.Name);
        }
        public override int GetHashCode()
        {
            int hash = 23 * 31 + Id;
            return hash * 31 + Name.GetHashCode();
        }
    }
}
