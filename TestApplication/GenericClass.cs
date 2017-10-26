namespace TestApplication
{
    public class GenericClass<T>
    {
        public T GetDefault(T input)
        {
            return default(T);
        }
    }
}
