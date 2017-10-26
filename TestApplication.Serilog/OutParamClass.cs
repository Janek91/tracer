namespace TestApplication
{
    public class OutParamClass
    {
        public string SetParamString(string input, out string mypara)
        {
            mypara = input;
            return string.Concat(input, input);
        }

        public void SetParamInt(string input, out int mypara)
        {
            mypara = int.Parse(input);
        }
    }
}
